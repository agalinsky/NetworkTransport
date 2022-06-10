namespace NetworkTransport.Pools
{
    public interface IPoolableBuffer
    {
        void InitBuffer(int length);
        int GetBufferLength();
        void Recycle();
    }
}
