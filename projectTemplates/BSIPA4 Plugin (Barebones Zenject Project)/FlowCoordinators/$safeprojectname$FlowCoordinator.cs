using BeatSaberMarkupLanguage;
using SiraUtil.Logging;

namespace $safeprojectname$.FlowCoordinators;
{
    internal class $safeprojectname$FlowCoordinator
    {

        private SiraLog _siraLog;
        public void Construct(MainFlowCoordinator mainFlowCoordinator, SiraLog siraLog)
        {
            _mainFlowCoordinator = mainFlowCoordinator;
            _siraLog = _siraLog;

        }
        
        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            try
            {
                if (firstActivation)
                {
                    SetTitle("Name of the Menu Button :)");
                    showBackButton = true;
                    ProvideInitialViewControllers( /* Put your ViewControllers here! */);
                }
            }
            catch (Exception ex)
            {
                _siraLog.Error(ex);
            }
                    
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            _mainFlowCoordinator.DismissFlowCoordinator(this);
        }
    }
}

