using ProjectAtlas.Application;
using UnityEngine;
using TMPro;

namespace ProjectAtlas.Presentation
{
    public sealed class InteractionTarget : MonoBehaviour, IInteractable
    {
        public string action = "catalog", destination, projectId, label = "Buka katalog";
        public TMP_Text titleLabel;
        public string Prompt => label;
        public void Interact(IAtlasHost host)
        {
            switch (action)
            {
                case "zone": host.Navigate(destination); break;
                case "project": host.OpenProject(projectId); break;
                case "next": host.NextGalleryPage(1); break;
                case "previous": host.NextGalleryPage(-1); break;
                case "archive": host.OpenArchive(); break;
                case "exhibition": host.OpenExhibitions(); break;
                default: host.OpenCatalog(); break;
            }
        }
    }
}
