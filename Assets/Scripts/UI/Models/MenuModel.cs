using UnityEngine;
using UnityUI.Commands;
using UnityUI.ViewModels;

namespace UnityUI.Models
{
    public class MenuModel
    {
        public bool HasSavedGame { get; set; }

        public string LatestSavedGameName { get; set; }

        public UnityCommand NewPartyCommand { get; }
        public UnityCommand ContinueCommand { get; }
        public UnityCommand OptionsCommand { get; }
        public UnityCommand ExitCommand { get; }

    }

}