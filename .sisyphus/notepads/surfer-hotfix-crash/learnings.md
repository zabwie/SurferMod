
- 2026-06-01: Fixed NullReferenceException in CustomLoadingBarManager.ToggleLoadingBar and SetLoadingPercent. Added null guards for LoadingBarManager.Instance and .loadingBar before access. Root cause: LoadingBarManager.Instance is null in lobby when ClientPatch.ExitGame postfix fires ToggleLoadingBar(false). Build passed with 0 errors.
