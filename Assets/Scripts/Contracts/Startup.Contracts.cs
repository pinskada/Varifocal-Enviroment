
namespace Contracts
{
    public interface ISceneManagement
    {
        // Sends a list of configuration file names to the GUI for display
        public void LoadCalibScene();
        public void PreviousScene();
        public void NextScene();
    }
}