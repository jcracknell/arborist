using System.Collections.Concurrent;
using System.Text;

namespace Arborist.CodeGen;

public sealed class PooledStringWriter : TextWriter {
    private static readonly ConcurrentQueue<PooledStringWriter> _instances = new();

    public static PooledStringWriter Rent() =>
        _instances.TryDequeue(out var instance) ? instance : new();

    private char[] _buffer;

    private PooledStringWriter() : base() {
        _buffer = new char[4096];
        Length = 0;
    }

    public override Encoding Encoding => Encoding.Unicode;
    public override string NewLine => "\n";

    public int Length { get; private set; }

    public char this[int index] => Get(index);

    private char Get(int index) {
        if(index < 0 || Length <= index)
            throw new IndexOutOfRangeException();

        return _buffer[index];
    }

    public ReadOnlySpan<char> AsSpan() =>
        _buffer.AsSpan(0, Length);

    public override string ToString() =>
        new string(_buffer, 0, Length);

    private void EnsureCapacity(int required) {
        var capacity = _buffer.Length;
        var requiredTotal = Length + required;
        if(capacity < requiredTotal) {
            do { capacity *= 2; } while(capacity < requiredTotal);
            Array.Resize(ref _buffer, capacity);
        }
    }

    protected override void Dispose(bool disposing) {
        if(disposing) {
            Clear();
            _instances.Enqueue(this);
        }
    }

    public override void Close() {
        Dispose(true);
    }

    public void Clear() {
        Length = 0;
    }

    public override void Flush() { }

    public override Task FlushAsync() {
        return Task.CompletedTask;
    }

    public void Write(ReadOnlySpan<char> buffer) {
        EnsureCapacity(buffer.Length);
        buffer.CopyTo(_buffer.AsSpan(Length));
        Length += buffer.Length;
    }

    public override void Write(char value) {
        EnsureCapacity(1);
        _buffer[Length++] = value;
    }

    public override void Write(char[] buffer, int index, int count) {
        Write(buffer.AsSpan(index, count));
    }

    public override void Write(string? value) {
        if(value is not null)
            Write(value.AsSpan());
    }

    public override Task WriteAsync(char value) {
        Write(value);
        return Task.CompletedTask;
    }

    public override Task WriteAsync(string? value) {
        Write(value);
        return Task.CompletedTask;
    }

    public override Task WriteAsync(char[] buffer, int index, int count) {
        Write(buffer, index, count);
        return Task.CompletedTask;
    }

    public override Task WriteLineAsync(char value) {
        WriteLine(value);
        return Task.CompletedTask;
    }

    public override Task WriteLineAsync(string? value) {
        WriteLine(value);
        return Task.CompletedTask;
    }

    public override Task WriteLineAsync(char[] buffer, int index, int count) {
        WriteLine(buffer, index, count);
        return Task.CompletedTask;
    }
}
