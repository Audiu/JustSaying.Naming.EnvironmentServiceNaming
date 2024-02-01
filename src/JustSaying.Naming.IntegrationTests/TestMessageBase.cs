using JustSaying.Models;

namespace JustSaying.Naming.IntegrationTests;

public abstract class TestMessageBase : Message
{
    public string UniqueId { get; }

    protected TestMessageBase()
    {
    }

    protected TestMessageBase(string uniqueId)
    {
        UniqueId = uniqueId;
    }
}
