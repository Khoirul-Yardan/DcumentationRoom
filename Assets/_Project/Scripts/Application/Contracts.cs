using ProjectAtlas.Domain;

namespace ProjectAtlas.Application
{
    public interface IProjectRepository { Catalog Load(); }
    public interface IAtlasHost
    {
        Catalog Catalog { get; }
        string Zone { get; }
        void Navigate(string scene);
        void OpenCatalog();
        void OpenProject(string id);
        void SetModal(bool open);
        void Resume();
        void NextGalleryPage(int direction);
        UserPreferences Preferences { get; }
        void SavePreferences();
        void ShowInGallery(string id);
        void OpenArchive();
        void OpenExhibitions();
        IVisitorSession Session { get; }
    }
    public interface IInteractable { string Prompt { get; } void Interact(IAtlasHost host); }
}
