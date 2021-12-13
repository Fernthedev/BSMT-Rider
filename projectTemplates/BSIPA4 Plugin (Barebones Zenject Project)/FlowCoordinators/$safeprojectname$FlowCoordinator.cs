using BeatSaberMarkupLanguage;
using SiraUtil.Logging;
using HMUI;
using System;

namespace $safeprojectname$.FlowCoordinators
{
    internal class $safeprojectname$FlowCoordinator : FlowCoordinator
    {
        private SiraLog _siraLog;
		private MainFlowCoordinator _mainFlowCoordinator;		

        public void Construct(MainFlowCoordinator mainFlowCoordinator, SiraLog siraLog)
        {
            _mainFlowCoordinator = mainFlowCoordinator;
            _siraLog = _siraLog; // SiraLog is preferred by a lot of people, as it
								 // injects context into the logs.
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

