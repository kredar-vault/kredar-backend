using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace Kredar.API.Checkout;

/// <summary>
/// In-memory pub/sub for checkout SSE streams. NombaWebhookService publishes after reconciliation;
/// CheckoutController subscribes per token. Singleton — survives requests.
/// </summary>
public class CheckoutEventBus
{
    private readonly ConcurrentDictionary<Guid, List<Channel<CheckoutSnapshot>>> _channels = new();

    public void Publish(Guid accountId, CheckoutSnapshot snapshot)
    {
        if (!_channels.TryGetValue(accountId, out var channels)) return;
        lock (channels)
        {
            foreach (var ch in channels)
                ch.Writer.TryWrite(snapshot);
        }
    }

    public async IAsyncEnumerable<CheckoutSnapshot> SubscribeAsync(
        Guid accountId, [EnumeratorCancellation] CancellationToken ct)
    {
        var channel = Channel.CreateBounded<CheckoutSnapshot>(new BoundedChannelOptions(16)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        var list = _channels.GetOrAdd(accountId, _ => []);
        lock (list) list.Add(channel);

        try
        {
            await foreach (var snapshot in channel.Reader.ReadAllAsync(ct))
                yield return snapshot;
        }
        finally
        {
            lock (list) list.Remove(channel);
            if (list.Count == 0) _channels.TryRemove(accountId, out _);
        }
    }
}
