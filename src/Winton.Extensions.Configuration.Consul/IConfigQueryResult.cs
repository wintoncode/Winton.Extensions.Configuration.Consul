namespace Winton.Extensions.Configuration.Consul
{
    internal interface IConfigQueryResult
    {
        bool Exists { get; }

        byte[] Value { get; }
    }
}