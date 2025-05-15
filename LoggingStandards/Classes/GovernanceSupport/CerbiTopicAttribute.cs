using System;

namespace Cerbi.Governance
{
    /// <summary>
    /// Used to annotate classes with a governance topic.
    /// This helps analyzers associate logs with governance profiles.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class CerbiTopicAttribute : Attribute
    {
        public string TopicName { get; }

        public CerbiTopicAttribute(string topicName)
        {
            TopicName = topicName;
        }
    }
}
