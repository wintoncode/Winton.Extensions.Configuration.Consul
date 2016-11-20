namespace Chocolate.AspNetCore.Configuration.Consul
{
    internal interface IConfigQueryResult
    {
        bool Exists { get; }

        byte[] Value { get; }
    }
}