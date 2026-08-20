using System;
using Mentality.UI.Repositories;
using VContainer.Unity;

namespace Mentality.UI.Services
{
    public class MenuSettingsService : IInitializable, IDisposable
    {
        private readonly UIService _uiService;
        private readonly MenuSettingsRepository _menuRepository;
        public void ReturntoMenu() => _menuRepository.ReturntoMenu(_uiService);

        public MenuSettingsService(MenuSettingsRepository menuRepository,UIService uiService)
        {
            _uiService = uiService;
            _menuRepository = menuRepository;
        }

        public void Initialize()
        {
        }

        public void Dispose()
        {
        }
    }
}