
namespace Contracts
{
    public interface IMainThreadQueue
    {
        // Enqueue an action to be executed on the main thread
        public void Enqueue(System.Action a);
    }
}
