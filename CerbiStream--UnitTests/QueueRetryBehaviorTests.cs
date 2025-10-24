using System;
using System.Threading.Tasks;
using Moq;
using Xunit;
using CerbiClientLogging.Interfaces.SendMessage;
using CerbiClientLogging.Implementations;
using CerbiClientLogging.Interfaces;
using CerbiStream.Logging.Configuration;
using CerbiStream.Interfaces;
using Microsoft.Extensions.Logging;

namespace CerbiStream.Tests
{
 public class QueueRetryBehaviorTests
 {
 [Fact(DisplayName = "Queue retry - permanent failure invokes configured retries and returns false")]
 public async Task QueueRetry_PermanentFailure_InvokesRetriesAndReturnsFalse()
 {
 var mockQueue = new Mock<ISendMessage>();
 var mockJson = new Mock<IConvertToJson>();
 var mockEncrypt = new Mock<IEncryption>();

 // Always throw
 mockQueue.Setup(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
 .ThrowsAsync(new Exception("Permanent failure"));

 mockJson.Setup(j => j.ConvertMessageToJson(It.IsAny<object>())).Returns("{}");
 mockEncrypt.Setup(e => e.IsEnabled).Returns(false);

 int retryCount =2;
 var options = new CerbiStreamOptions().WithQueueRetries(true, retryCount: retryCount, delayMilliseconds:1);

 var logger = new CerbiClientLogging.Implementations.Logging(mockQueue.Object, mockJson.Object, mockEncrypt.Object, options);

 var result = await logger.LogEventAsync("retry-perm", LogLevel.Error);

 // Should have attempted retryCount +1 times
 mockQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(retryCount +1));
 Assert.False(result);
 }

 [Fact(DisplayName = "Queue retry - transient failure succeeds on retry")]
 public async Task QueueRetry_TransientFailure_SucceedsOnRetry()
 {
 var mockQueue = new Mock<ISendMessage>();
 var mockJson = new Mock<IConvertToJson>();
 var mockEncrypt = new Mock<IEncryption>();

 // First throw, then succeed
 mockQueue.SetupSequence(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()))
 .ThrowsAsync(new Exception("Transient"))
 .ReturnsAsync(true);

 mockJson.Setup(j => j.ConvertMessageToJson(It.IsAny<object>())).Returns("{}");
 mockEncrypt.Setup(e => e.IsEnabled).Returns(false);

 var options = new CerbiStreamOptions().WithQueueRetries(true, retryCount:3, delayMilliseconds:1);
 var logger = new CerbiClientLogging.Implementations.Logging(mockQueue.Object, mockJson.Object, mockEncrypt.Object, options);

 var result = await logger.LogEventAsync("retry-transient", LogLevel.Warning);

 mockQueue.Verify(q => q.SendMessageAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Exactly(2));
 Assert.True(result);
 }
 }
}
