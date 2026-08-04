using UnityEngine;
using UnityUI.Commands;
using UnityUI.ViewModels;

namespace UnityUI.Models
{
    public class MenuModel
    {
        public bool HasSavedGame { get; set; }

        public string LatestSavedGameName { get; set; }

        public UnityHandler NewPartyHandler { get; }
        public UnityHandler ContinueHandler { get; }
        public UnityHandler OptionsHandler { get; }
        public UnityHandler ExitHandler { get; }

    }

}