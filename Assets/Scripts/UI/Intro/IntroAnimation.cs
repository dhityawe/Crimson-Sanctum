using DG.Tweening;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class IntroAnimation : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup; // Canvas Group untuk seluruh container
    [SerializeField] private CanvasGroup logoCanvasGroup; // Canvas Group khusus untuk logo
    [SerializeField] private RectTransform rectTransform; // Logo
    [SerializeField] private RectTransform backgroundRectTransform; // Background

    [Header("Pengaturan Intro (Fade In)")]
    public float introDuration = 0.5f; // Durasi Fade In Logo
    public Ease introEase = Ease.Linear;

    [Header("Pengaturan Dissolve (Fade Out & Move Left)")]
    public float dissolveDuration = 1.0f; // Durasi Dissolve Out
    public Vector2 moveDistance = new(-500f, 0f); // Jarak pergerakan ke kiri (Anchor Position)
    public Ease dissolveEase = Ease.Linear;

    [Header("Pengaturan Scene Management")]
    [SerializeField, Scene] private string nextSceneName = ""; // Nama scene yang akan di-load
    [SerializeField] private bool unloadCurrentScene = true; // Apakah unload scene saat ini setelah load scene baru

    void Start()
    {
        // Mendapatkan komponen
        // canvasGroup = GetComponent<CanvasGroup>();
        // rectTransform = GetComponent<RectTransform>();

        // if (canvasGroup == null || rectTransform == null)
        // {
        //     Debug.LogError("Objek memerlukan CanvasGroup dan RectTransform!");
        //     return;
        // }

        // Jalankan seluruh Sequence animasi
        PlayFullAnimation();
    }

    public void PlayFullAnimation()
    {
        // --- 1. SET UP KONDISI AWAL (SEBELUM INTRO DIMULAI) ---
        
        // Simpan posisi awal logo (akan menjadi posisi tengah/akhir intro)
        Vector2 originalPosition = rectTransform.anchoredPosition;
        
        // Atur kondisi awal untuk Intro:
        // - Background visible (alpha = 1)
        // - Logo invisible (alpha = 0), akan fade in
        canvasGroup.alpha = 1f;
        rectTransform.localScale = Vector3.one; // Logo scale tetap 1
        
        if (logoCanvasGroup != null)
        {
            logoCanvasGroup.alpha = 0f; // Logo mulai dari transparan
        }


        // --- 2. BUAT SEQUENCE UNTUK SELURUH ANIMASI ---

        Sequence logoSequence = DOTween.Sequence();


        // ----------------------------------------------------------------
        // PHASE 1: INTRO (FADE IN LOGO)
        // Background sudah visible dari awal, hanya logo yang fade in
        // ----------------------------------------------------------------

        // Tween 1: Fade In Logo (Dari 0 ke 1)
        if (logoCanvasGroup != null)
        {
            logoSequence.Append(
                logoCanvasGroup.DOFade(1f, introDuration)
                .SetEase(introEase)
            );
        }

        // Opsi: Tambahkan jeda sebentar sebelum Dissolve dimulai
        logoSequence.AppendInterval(0.5f);


        // ----------------------------------------------------------------
        // PHASE 2: DISSOLVE (FADE OUT & MOVE LEFT)
        // Logo dan Background bergerak ke kiri bersamaan sambil fade out
        // ----------------------------------------------------------------
        
        // MULAI LOAD SCENE BARU SAAT DISSOLVE DIMULAI
        logoSequence.AppendCallback(() =>
        {
            if (!string.IsNullOrEmpty(nextSceneName))
            {
                Debug.Log("Dissolve dimulai, mulai load scene baru...");
                StartCoroutine(LoadNextScene());
            }
            else
            {
                Debug.LogWarning("Next Scene Name belum diisi! Tidak ada scene yang di-load.");
            }
        });
        
        // Tentukan posisi akhir Dissolve untuk logo
        Vector2 finalDissolvePosition = originalPosition + moveDistance;

        // Tween 2A: Move Left - LOGO (Dari posisi awal ke posisi akhir Dissolve)
        logoSequence.Append(
            rectTransform.DOAnchorPos(finalDissolvePosition, dissolveDuration)
            .SetEase(dissolveEase)
        );

        // Tween 2B: Move Left - BACKGROUND (Bergerak bersamaan dengan logo)
        if (backgroundRectTransform != null)
        {
            Vector2 bgOriginalPosition = backgroundRectTransform.anchoredPosition;
            Vector2 bgFinalPosition = bgOriginalPosition + moveDistance;
            
            logoSequence.Join(
                backgroundRectTransform.DOAnchorPos(bgFinalPosition, dissolveDuration)
                .SetEase(dissolveEase)
            );
        }

        // Tween 2C: Fade Out (Dissolve) (Dari 1 ke 0) - Berjalan bersamaan dengan pergerakan
        logoSequence.Join(
            canvasGroup.DOFade(0f, dissolveDuration)
            .SetEase(dissolveEase)
        );
        
        
        // ----------------------------------------------------------------
        // PHASE 3: SETELAH DISSOLVE SELESAI
        // ----------------------------------------------------------------
        
        // Tambahkan callback saat Sequence selesai - UNLOAD SCENE LAMA
        logoSequence.OnComplete(() =>
        {
            Debug.Log("Animasi Dissolve Selesai!");
            
            // Unload scene lama setelah dissolve selesai
            if (unloadCurrentScene && !string.IsNullOrEmpty(nextSceneName))
            {
                StartCoroutine(UnloadCurrentScene());
            }
        });
    }

    /// <summary>
    /// Coroutine untuk load scene baru secara additive
    /// Dipanggil saat dissolve MULAI
    /// </summary>
    private IEnumerator LoadNextScene()
    {
        Debug.Log($"Memulai load scene: {nextSceneName} (Additive)");
        
        // Load scene baru secara additive
        AsyncOperation loadOperation = SceneManager.LoadSceneAsync(nextSceneName, LoadSceneMode.Additive);
        
        // Tunggu sampai scene selesai di-load
        while (!loadOperation.isDone)
        {
            // Optional: Bisa tambahkan loading progress bar di sini
            // float progress = loadOperation.progress;
            yield return null;
        }
        
        Debug.Log($"Scene {nextSceneName} berhasil di-load!");
        
        // Set scene baru sebagai active scene
        Scene newScene = SceneManager.GetSceneByName(nextSceneName);
        if (newScene.IsValid())
        {
            SceneManager.SetActiveScene(newScene);
            Debug.Log($"Scene {nextSceneName} di-set sebagai active scene");
        }
    }

    /// <summary>
    /// Coroutine untuk unload scene lama
    /// Dipanggil saat dissolve SELESAI
    /// </summary>
    private IEnumerator UnloadCurrentScene()
    {
        yield return new WaitForSeconds(0.5f);
        
        // Simpan nama scene saat ini untuk di-unload
        string currentSceneName = gameObject.scene.name;
        
        Debug.Log($"Memulai unload scene: {currentSceneName}");
        
        AsyncOperation unloadOperation = SceneManager.UnloadSceneAsync(currentSceneName);
        
        // Tunggu sampai scene selesai di-unload
        while (unloadOperation != null && !unloadOperation.isDone)
        {
            yield return null;
        }
        
        Debug.Log($"Scene {currentSceneName} berhasil di-unload!");
    }
}
