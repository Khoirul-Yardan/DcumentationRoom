using ProjectAtlas.Domain;
using UnityEngine;
using TMPro;
using System.Collections;
using System.Linq;

namespace ProjectAtlas.Presentation
{
    public sealed class GalleryPresenter : MonoBehaviour
    {
        public InteractionTarget[] slots;
        public TMP_Text pageLabel;
        public int PageIndex { get; private set; }
        public void Bind(Catalog catalog, int page)
        {
            StopAllCoroutines();
            foreach (var slot in slots) slot.GetComponent<StandCover>()?.Cleanup();
            var all = CatalogQuery.Search(catalog);
            PageIndex = Mathf.Clamp(page, 0, Mathf.Max(0, (all.Length - 1) / 8));
            var items = CatalogQuery.Page(all, PageIndex);
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i].gameObject.SetActive(i < items.Length);
                if (i >= items.Length) continue;
                var p = items[i];
                slots[i].projectId = p.id;
                slots[i].label = "Baca " + p.title;
                slots[i].titleLabel.richText = false;
                slots[i].titleLabel.text = (p.isSample ? "SINTETIS / " : "") + p.year + " / " + p.developmentStatus + "\n" + p.title;
                slots[i].titleLabel.transform.localPosition = new Vector3(0, .69f, .75f);
                slots[i].titleLabel.rectTransform.sizeDelta = new Vector2(4.1f, .75f);
                if (UnityEngine.Application.isPlaying && slots[i].GetComponent<StandCover>() == null) slots[i].gameObject.AddComponent<StandCover>().Initialize();
            }
            pageLabel.text = all.Length == 0 ? "Belum ada proyek" : $"{PageIndex * 8 + 1}-{PageIndex * 8 + items.Length} dari {all.Length} proyek";
            if (UnityEngine.Application.isPlaying) StartCoroutine(Covers(catalog, items));
        }
        IEnumerator Covers(Catalog catalog, Project[] items)
        {
            // Sequential decode keeps temporary allocations bounded when rebinding eight slots.
            for (int i = 0; i < items.Length && i < slots.Length; i++)
                yield return slots[i].GetComponent<StandCover>().Load(catalog.media.FirstOrDefault(m => m.id == items[i].coverMediaId));
        }
    }
}
