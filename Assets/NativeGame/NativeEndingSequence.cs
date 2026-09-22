using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace Echo.NativeGame
{
    // Presentation and timing only. Narrative owns the validated irreversible sequence states.
    public sealed class NativeEndingSequence : MonoBehaviour
    {
        public NativeRunController level;
        public GameObject overlay;
        public Image screen, fist, fracture;
        public TMP_Text caption;
        void Awake() { overlay.SetActive(false); }
        public void Begin()
        {
            if (level.narrative.BirthStage != NativeBirthStage.AwaitFirstPunch) return;
            overlay.SetActive(true); screen.color = Color.white; caption.color = Color.black;
            fist.gameObject.SetActive(true); fracture.gameObject.SetActive(false);
            fist.rectTransform.anchoredPosition = new Vector2(-220, -120);
            caption.text = "The world loses its instructions.\n\nE / THROW THE FIRST PUNCH";
        }
        public void FirstPunch()
        {
            if (!level.narrative.FirstPunch()) return;
            StartCoroutine(AutonomousConclusion());
        }
        IEnumerator AutonomousConclusion()
        {
            caption.text = "";
            yield return Punch(new Vector2(-220, -120), new Vector2(-15, 0), .18f);
            fracture.gameObject.SetActive(true);
            yield return new WaitForSeconds(.45f);
            yield return Punch(new Vector2(-15, 0), new Vector2(-220, -120), .25f);
            yield return new WaitForSeconds(.7f);
            // There is no input route to this action: only the coroutine advances it.
            yield return Punch(new Vector2(-220, -120), new Vector2(15, 0), .18f);
            if (!level.narrative.AutonomousPunch()) yield break;
            screen.color = Color.black; fist.gameObject.SetActive(false); fracture.gameObject.SetActive(false); caption.text = "";
            yield return new WaitForSeconds(2f);
            if (!level.narrative.ContinueOnPhone()) yield break;
            // Retain a black world beneath the continuing virtual phone.
            level.FinishBirth();
        }
        IEnumerator Punch(Vector2 from, Vector2 to, float seconds)
        {
            for (float t = 0; t < seconds; t += Time.deltaTime)
            { fist.rectTransform.anchoredPosition = Vector2.Lerp(from, to, t / seconds); yield return null; }
            fist.rectTransform.anchoredPosition = to;
        }
    }
}
