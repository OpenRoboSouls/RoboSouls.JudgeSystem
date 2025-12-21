using System;
using System.Collections.Concurrent;

namespace RoboSouls.JudgeSystem;

public interface ICacheReader<out T>
{
    public T Load(int key);
    public bool Exists(int key);
}

public interface ICacheWriter<in T>
{
    public void Save(int key, T value);
    public void Delete(int key);
    public void SaveWithTimeout(int key, T value, TimeSpan timeout);
}

public interface ICacheProvider<T> : ICacheReader<T>, ICacheWriter<T>;

public static class CacheExtension
{
    extension<T>(ICacheReader<T> reader)
    {
        public bool TryLoad(int key, out T value)
        {
            if (reader.Exists(key))
            {
                value = reader.Load(key);
                return true;
            }
            else
            {
                value = default;
                return false;
            }
        
        }

        public T LoadOrDefault(int key, T defaultValue)
        {
            if (TryLoad(reader, key, out var value))
            {
                return value;
            }
            else
            {
                return defaultValue;
            }
        }

        public ICacheReader<T> WithReaderNamespace(int mask)
        {
            // return new MaskedCacheProvider<T>(reader, mask);
            return MaskedCacheProvider<T>.Get(reader, mask);
        }

        public ICacheReader<T> WithReaderNamespace(in Identity nameSpace
        )
        {
            return reader.WithReaderNamespace(nameSpace.GetHashCode());
        }
    }

    extension<T>(ICacheWriter<T> writer)
    {
        public ICacheWriter<T> WithWriterNamespace(int mask)
        {
            // return new MaskedCacheProvider<T>(writer, mask);
            return MaskedCacheProvider<T>.Get(writer, mask);
        }

        public ICacheWriter<T> WithWriterNamespace(in Identity nameSpace
        )
        {
            return writer.WithWriterNamespace(nameSpace.GetHashCode());
        }
    }
}

public enum CacheActionType : byte
{
    Save,
    Delete,
}

public struct CacheAction<T>
{
    public CacheActionType ActionType;
    public int Key;
    public T Payload;
}

public class MaskedCacheProvider<T> : ICacheProvider<T>
{
    // public MaskedCacheProvider(ICacheReader<T> reader, int mask)
    // {
    //     _reader = reader;
    //     _mask = mask;
    // }
    //
    // public MaskedCacheProvider(ICacheWriter<T> writer, int mask)
    // {
    //     _writer = writer;
    //     _mask = mask;
    // }

    // optimized version:
    // use pooling to avoid memory allocation
    private static readonly ConcurrentDictionary<
        (ICacheReader<T>, int),
        MaskedCacheProvider<T>
    > ReaderPool = new ConcurrentDictionary<(ICacheReader<T>, int), MaskedCacheProvider<T>>();

    private static readonly ConcurrentDictionary<
        (ICacheWriter<T>, int),
        MaskedCacheProvider<T>
    > WriterPool = new ConcurrentDictionary<(ICacheWriter<T>, int), MaskedCacheProvider<T>>();

    private readonly int _mask;
    private readonly ICacheReader<T> _reader = null;
    private readonly ICacheWriter<T> _writer = null;

    private MaskedCacheProvider(ICacheReader<T> reader, int mask)
    {
        _reader = reader;
        _mask = mask;
    }

    private MaskedCacheProvider(ICacheWriter<T> writer, int mask)
    {
        _writer = writer;
        _mask = mask;
    }

    public T Load(int key)
    {
        return _reader.Load(GetMaskedKey(key));
    }

    public bool Exists(int key)
    {
        return _reader.Exists(GetMaskedKey(key));
    }

    public void Save(int key, T value)
    {
        _writer.Save(GetMaskedKey(key), value);
    }

    public void Delete(int key)
    {
        _writer.Delete(GetMaskedKey(key));
    }

    public void SaveWithTimeout(int key, T value, TimeSpan timeout)
    {
        _writer.SaveWithTimeout(GetMaskedKey(key), value, timeout);
    }

    public static MaskedCacheProvider<T> Get(ICacheReader<T> reader, int mask)
    {
        return ReaderPool.GetOrAdd(
            (reader, mask),
            pair => new MaskedCacheProvider<T>(pair.Item1, pair.Item2)
        );
    }

    public static MaskedCacheProvider<T> Get(ICacheWriter<T> writer, int mask)
    {
        return WriterPool.GetOrAdd(
            (writer, mask),
            pair => new MaskedCacheProvider<T>(pair.Item1, pair.Item2)
        );
    }

    private int GetMaskedKey(int key)
    {
        return key ^ _mask;
    }
}