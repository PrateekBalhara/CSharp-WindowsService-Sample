namespace SampleCode
{
    public interface IServiceClass
    {
        Task ProcessQueue();
        Task ProcessCart(int headerId, UserEntity ownerUser, UserEntity submitUser);
    }
}