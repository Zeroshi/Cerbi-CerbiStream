# CerbiStream Python SDK - Scoring Contract Implementation

**Package:** `cerbistream`  
**Target:** Python 3.9+  
**Date:** 2026-02-03  
**Status:** ?? Implementation Guide

---

## Overview

Python SDK for CerbiStream. The SDK sends violations to the Scoring API - it does **NOT** compute scores.

**Key Principle:** Scoring is centralized in the Scoring API. SDKs only send violations with severities defined in `cerbi_governance.json`.

---

## Architecture

```
logger.info("...")
         ?
         ?
???????????????????????????????????????
?      CerbiStreamHandler             ?
?    - Intercepts logging calls       ?
?    - Runs governance validation     ?
?    - Extracts violations + severity ?
?    - Sends to queue                 ?
???????????????????????????????????????
         ?
         ?  ScoringEventDto (Score = None)
         ?
???????????????????????????????????????
?         Scoring API                 ?
?    - Reads governance config        ?
?    - Computes scores centrally      ?
???????????????????????????????????????
```

---

## What Gets Sent

```json
{
  "SchemaVersion": "1.0",
  "TenantId": "my-tenant",
  "AppName": "my-python-app",
  "LogId": "uuid",
  "Score": null,
  "Violations": [
    {
      "RuleId": "PII-001",
      "Severity": "Warning"
    }
  ]
}
```

- `Score` is **null** - Scoring API computes it
- `Severity` comes from governance config

---

## Package Structure

```
cerbistream/
??? __init__.py
??? handler.py
??? options.py
??? dto.py
??? governance/
?   ??? __init__.py
?   ??? validator.py
??? queue/
    ??? __init__.py
    ??? base.py
    ??? azure_servicebus.py
```

---

## Implementation

### 1. DTO Classes

**File:** `cerbistream/dto.py`

```python
"""ScoringEventDto - matches CerbiShield.Contracts"""

from dataclasses import dataclass, field
from datetime import datetime
from typing import Any, Dict, List, Optional
import uuid
import json

SCHEMA_VERSION = "1.0"


@dataclass
class ViolationDto:
    """Violation from governance validation. Severity comes from config."""
    rule_id: Optional[str] = None
    code: Optional[str] = None
    field_name: Optional[str] = None
    severity: Optional[str] = None  # FROM GOVERNANCE CONFIG
    message: Optional[str] = None

    def to_dict(self) -> Dict[str, Any]:
        return {
            "RuleId": self.rule_id,
            "Code": self.code,
            "Field": self.field_name,
            "Severity": self.severity,
            "Message": self.message
        }


@dataclass
class GovernanceFlagsDto:
    governance_relaxed: bool = False

    def to_dict(self) -> Dict[str, bool]:
        return {"GovernanceRelaxed": self.governance_relaxed}


@dataclass
class ScoringEventDto:
    """
    Scoring event DTO.
    
    IMPORTANT: Score is None - Scoring API computes it.
    Violations have severities from governance config.
    """
    tenant_id: str
    app_name: str
    log_id: str = field(default_factory=lambda: str(uuid.uuid4()))
    schema_version: str = SCHEMA_VERSION
    environment: Optional[str] = None
    runtime: str = "python"
    correlation_id: Optional[str] = None
    timestamp_utc: datetime = field(default_factory=datetime.utcnow)
    governance_profile: str = "default"
    log_level: str = "Information"
    
    # Score is None - Scoring API computes it
    score: None = None
    
    violations: List[ViolationDto] = field(default_factory=list)
    governance_flags: Optional[GovernanceFlagsDto] = None
    raw_payload: Optional[Dict[str, Any]] = None

    def to_dict(self) -> Dict[str, Any]:
        return {
            "SchemaVersion": self.schema_version,
            "TenantId": self.tenant_id,
            "AppName": self.app_name,
            "Environment": self.environment,
            "Runtime": self.runtime,
            "LogId": self.log_id,
            "CorrelationId": self.correlation_id,
            "TimestampUtc": self.timestamp_utc.isoformat() + "Z",
            "GovernanceProfile": self.governance_profile,
            "LogLevel": self.log_level,
            "Score": None,  # Scoring API computes this
            "Violations": [v.to_dict() for v in self.violations],
            "GovernanceFlags": self.governance_flags.to_dict() if self.governance_flags else None,
            "RawPayload": self.raw_payload
        }

    def to_json(self) -> str:
        return json.dumps(self.to_dict(), default=str)
```

### 2. Options Class

**File:** `cerbistream/options.py`

```python
"""Configuration for CerbiStream - NO scoring options."""

from dataclasses import dataclass
from typing import Optional


@dataclass
class CerbiStreamOptions:
    """
    Configuration options.
    
    Note: NO scoring configuration here.
    Scoring rules are in cerbi_governance.json.
    Score computation happens in Scoring API.
    """
    # Required
    tenant_id: str
    app_name: str
    queue_connection_string: str
    
    # Optional
    queue_name: str = "cerbishield.log-scoring"
    environment: Optional[str] = None
    governance_profile: str = "default"
    governance_config_path: str = "cerbi_governance.json"
    queue_type: str = "AzureServiceBus"
```

### 3. Governance Validator

**File:** `cerbistream/governance/validator.py`

```python
"""Governance validation using cerbi_governance.json"""

import json
from pathlib import Path
from typing import Any, Dict, List
from ..dto import ViolationDto


class GovernanceValidator:
    """
    Validates log data against governance rules.
    Severities come from config file, not hardcoded.
    """
    
    def __init__(self, config_path: str, profile: str = "default"):
        self.profile = profile
        self.rules = []
        self._load_config(config_path)
    
    def _load_config(self, config_path: str):
        """Load rules from cerbi_governance.json"""
        path = Path(config_path)
        if not path.exists():
            return
        
        with open(path) as f:
            config = json.load(f)
        
        profiles = config.get("profiles", {})
        profile_config = profiles.get(self.profile, {})
        self.rules = profile_config.get("rules", [])
    
    def validate(self, data: Dict[str, Any]) -> List[ViolationDto]:
        """
        Validate data against governance rules.
        Returns violations with severities FROM CONFIG.
        """
        violations = []
        
        for rule in self.rules:
            rule_id = rule.get("ruleId")
            code = rule.get("code")
            fields = rule.get("fields", [])
            severity = rule.get("severity")  # FROM CONFIG
            
            for field in fields:
                if self._field_exists(data, field):
                    violations.append(ViolationDto(
                        rule_id=rule_id,
                        code=code,
                        field_name=field,
                        severity=severity,  # FROM GOVERNANCE CONFIG
                        message=rule.get("message", f"Field '{field}' violates rule {rule_id}")
                    ))
        
        return violations
    
    def _field_exists(self, data: Dict[str, Any], field: str) -> bool:
        """Check if field exists in nested data"""
        parts = field.split(".")
        current = data
        for part in parts:
            if isinstance(current, dict) and part in current:
                current = current[part]
            else:
                return False
        return True
```

### 4. Handler Implementation

**File:** `cerbistream/handler.py`

```python
"""CerbiStream logging handler - sends violations, NOT scores."""

import json
import logging
from datetime import datetime
from typing import Any, Dict, List
import uuid

from .options import CerbiStreamOptions
from .dto import ScoringEventDto, ViolationDto, GovernanceFlagsDto
from .governance.validator import GovernanceValidator
from .queue.azure_servicebus import AzureServiceBusQueueSender


class CerbiStreamHandler(logging.Handler):
    """
    Logging handler that sends events to Scoring API.
    
    IMPORTANT: Does NOT compute scores.
    Scores are computed by Scoring API using governance config.
    """
    
    def __init__(
        self,
        tenant_id: str,
        app_name: str,
        queue_connection_string: str,
        queue_name: str = "cerbishield.log-scoring",
        environment: str = None,
        governance_profile: str = "default",
        governance_config_path: str = "cerbi_governance.json",
        level: int = logging.INFO
    ):
        super().__init__(level)
        
        self.options = CerbiStreamOptions(
            tenant_id=tenant_id,
            app_name=app_name,
            queue_connection_string=queue_connection_string,
            queue_name=queue_name,
            environment=environment,
            governance_profile=governance_profile,
            governance_config_path=governance_config_path
        )
        
        # Governance validator reads rules from config file
        self.validator = GovernanceValidator(
            governance_config_path, 
            governance_profile
        )
        
        self.queue_sender = AzureServiceBusQueueSender(
            queue_connection_string, 
            queue_name
        )
    
    def emit(self, record: logging.LogRecord) -> None:
        """Process log record and send to queue."""
        try:
            # Extract extra data
            extra = self._extract_extra(record)
            
            # Validate against governance rules
            # Severities come from cerbi_governance.json
            violations = self.validator.validate(extra)
            
            # Build DTO - Score is None
            event = ScoringEventDto(
                tenant_id=extra.get("tenant_id", self.options.tenant_id),
                app_name=extra.get("app_name", self.options.app_name),
                environment=extra.get("environment", self.options.environment),
                runtime="python",
                log_id=str(uuid.uuid4()),
                correlation_id=extra.get("correlation_id"),
                timestamp_utc=datetime.utcnow(),
                governance_profile=self.options.governance_profile,
                log_level=self._map_level(record.levelno),
                score=None,  # SCORING API COMPUTES THIS
                violations=violations,
                governance_flags=GovernanceFlagsDto(
                    governance_relaxed=extra.get("governance_relaxed", False)
                ),
                raw_payload=self._build_payload(record, extra)
            )
            
            # Send to queue
            self.queue_sender.send(event.to_json(), event.log_id)
            
        except Exception:
            self.handleError(record)
    
    def _map_level(self, levelno: int) -> str:
        if levelno >= logging.CRITICAL:
            return "Critical"
        elif levelno >= logging.ERROR:
            return "Error"
        elif levelno >= logging.WARNING:
            return "Warning"
        elif levelno >= logging.INFO:
            return "Information"
        return "Debug"
    
    def _extract_extra(self, record: logging.LogRecord) -> Dict[str, Any]:
        """Extract extra attributes from log record."""
        standard = {
            'name', 'msg', 'args', 'created', 'filename', 'funcName',
            'levelname', 'levelno', 'lineno', 'module', 'pathname',
            'process', 'processName', 'thread', 'threadName', 'message'
        }
        return {
            k: v for k, v in record.__dict__.items()
            if k not in standard and not k.startswith('_')
        }
    
    def _build_payload(self, record: logging.LogRecord, extra: Dict) -> Dict:
        return {
            "logger": record.name,
            "level": record.levelname,
            "message": record.getMessage(),
            "module": record.module,
            "function": record.funcName,
            **extra
        }
    
    def close(self) -> None:
        self.queue_sender.close()
        super().close()
```

---

## Usage

```python
import logging
from cerbistream import CerbiStreamHandler

handler = CerbiStreamHandler(
    tenant_id="my-tenant",
    app_name="my-python-app",
    environment="production",
    queue_connection_string=os.environ["SERVICEBUS_CONNECTION"],
    governance_profile="default",
    governance_config_path="cerbi_governance.json"
)

logger = logging.getLogger(__name__)
logger.addHandler(handler)

# Log with extra data - will be validated against governance rules
logger.info("User logged in", extra={
    "user_id": "12345",
    "email": "user@example.com"  # Will trigger PII violation if in config
})
```

**Developer does NOT configure:**
- Severity weights
- Score deductions
- Rule definitions

All scoring rules are in `cerbi_governance.json` and computed by Scoring API.

---

## Governance Config Example

**File:** `cerbi_governance.json`

```json
{
  "profiles": {
    "default": {
      "rules": [
        {
          "ruleId": "PII-001",
          "code": "PII",
          "fields": ["email", "ssn", "phone"],
          "severity": "Warning",
          "message": "PII field detected"
        },
        {
          "ruleId": "SEC-001",
          "code": "Security",
          "fields": ["password", "api_key", "secret"],
          "severity": "Critical",
          "message": "Security field detected"
        }
      ]
    }
  }
}
```

---
