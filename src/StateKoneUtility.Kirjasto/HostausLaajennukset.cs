using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StateKoneUtility.Kirjasto.Mallit;
using StateKoneUtility.Kirjasto.Rajapinnat;

namespace StateKoneUtility.Kirjasto
{
    public static class HostausLaajennukset
    {
        public static IServiceCollection AddVIDurableStateMachines(this IServiceCollection palvelut)
        {
            palvelut.TryAddKeyedScoped(typeof(IDurableStack<>), KeyedService.AnyKey, typeof(DurableStack<>));
            palvelut.TryAddKeyedScoped(typeof(IDurablePriorityQueue<,>), KeyedService.AnyKey, typeof(DurablePriorityQueue<,>));
            palvelut.TryAddKeyedScoped(typeof(IDurableListLookup<,>), KeyedService.AnyKey, typeof(DurableListLookup<,>));
            palvelut.TryAddKeyedScoped(typeof(IDurableSetLookup<,>), KeyedService.AnyKey, typeof(DurableSetLookup<,>));
            palvelut.TryAddKeyedScoped(typeof(IDurableTree<>), KeyedService.AnyKey, typeof(DurableTree<>));
            palvelut.TryAddKeyedScoped(typeof(IDurableGraph<,>), KeyedService.AnyKey, typeof(DurableGraph<,>));
            palvelut.TryAddKeyedScoped(typeof(IDurableCancellationTokenSource), KeyedService.AnyKey, typeof(DurableCancellationTokenSource));
            palvelut.TryAddKeyedScoped(typeof(IDurableObject<>), KeyedService.AnyKey, typeof(DurableObject<>));
            palvelut.TryAddKeyedScoped(typeof(IDurableOrderedSet<>), KeyedService.AnyKey, typeof(DurableOrderedSet<>));
            palvelut.TryAddKeyedScoped(typeof(IDurableOrderedSetLookup<,>), KeyedService.AnyKey, typeof(DurableOrderedSetLookup<,>));
            palvelut.TryAddKeyedScoped(typeof(IDurableRingBuffer<>), KeyedService.AnyKey, typeof(DurableRingBuffer<>));
            palvelut.TryAddKeyedScoped(typeof(IDurableRingBufferCollection<,>), KeyedService.AnyKey, typeof(DurableRingBufferCollection<,>));
            palvelut.TryAddKeyedScoped(typeof(IDurableTimeWindowBuffer<>), KeyedService.AnyKey, typeof(DurableTimeWindowBuffer<>));
            palvelut.TryAddKeyedScoped(typeof(IDurableTimeWindowBufferCollection<,>), KeyedService.AnyKey, typeof(DurableTimeWindowBufferCollection<,>));

            const string BinaryFormatKey = "orleans-binary";

            palvelut.TryAddKeyedSingleton(typeof(IDurableStackCommandCodec<>), BinaryFormatKey, typeof(DurableStackCommandBinaryCodec<>));
            palvelut.TryAddKeyedSingleton(typeof(IDurablePriorityQueueCommandCodec<,>), BinaryFormatKey, typeof(DurablePriorityQueueCommandCodec<,>));
            palvelut.TryAddKeyedSingleton(typeof(IDurableListLookupCommandCodec<,>), BinaryFormatKey, typeof(DurableListLookupCommandBinaryCodec<,>));
            palvelut.TryAddKeyedSingleton(typeof(IDurableSetLookupCommandCodec<,>), BinaryFormatKey, typeof(DurableSetLookupCommandBinaryCodec<,>));
            palvelut.TryAddKeyedSingleton(typeof(IDurableTreeCommandCodec<>), BinaryFormatKey, typeof(DurableTreeCommandBinaryCodec<>));
            palvelut.TryAddKeyedSingleton(typeof(IDurableGraphCommandCodec<,>), BinaryFormatKey, typeof(DurableGraphCommandBinaryCodec<,>));
            palvelut.TryAddKeyedSingleton(typeof(IDurableCancellationTokenSourceCommandCodec), BinaryFormatKey, typeof(DurableCancellationTokenSourceCommandBinaryCodec));
            palvelut.TryAddKeyedSingleton(typeof(IDurableObjectCommandCodec<>), BinaryFormatKey, typeof(DurableObjectCommandBinaryCodec<>));
            palvelut.TryAddKeyedSingleton(typeof(IDurableOrderedSetCommandCodec<>), BinaryFormatKey, typeof(DurableOrderedSetCommandBinaryCodec<>));
            palvelut.TryAddKeyedSingleton(typeof(IDurableOrderedSetLookupCommandCodec<,>), BinaryFormatKey, typeof(DurableOrderedSetLookupCommandBinaryCodec<,>));
            palvelut.TryAddKeyedSingleton(typeof(IDurableRingBufferCommandCodec<>), BinaryFormatKey, typeof(DurableRingBufferCommandBinaryCodec<>));
            palvelut.TryAddKeyedSingleton(typeof(IDurableRingBufferCollectionCommandCodec<,>), BinaryFormatKey, typeof(DurableRingBufferCollectionCommandBinaryCodec<,>));
            palvelut.TryAddKeyedSingleton(typeof(IDurableTimeWindowBufferCommandCodec<>), BinaryFormatKey, typeof(DurableTimeWindowBufferCommandBinaryCodec<>));
            palvelut.TryAddKeyedSingleton(typeof(IDurableTimeWindowBufferCollectionCommandCodec<,>), BinaryFormatKey, typeof(DurableTimeWindowBufferCollectionCommandBinaryCodec<,>));
            return palvelut;
        }
    }
}
