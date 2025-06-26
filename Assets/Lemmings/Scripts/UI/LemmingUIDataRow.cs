using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lemmings.UI
{
    /// <summary>
    /// Leaf view for a single Lemming in the Control Pane list.
    /// Displays the Lemming's name and confidence score/bar.
    /// </summary>
    [Icon("Assets/Input/Icons/LemmingPale.png")]
    public class LemmingUIDataRow : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private Image confidenceBar;
        [SerializeField] private TextMeshProUGUI confidenceText;

        private LemmingReference _reference;

        /// <summary>
        /// Bind the row to a LemmingReference model.
        /// </summary>
        public void Bind(LemmingReference reference)
        {
            _reference = reference;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (_reference.Source != null)
            {
                nameText.text = _reference.Name;
                Debug.Log($"Name: {_reference.Name} Reference: {_reference.Source}");
                float c = Mathf.Clamp01(_reference.confidence);

                // fill amount as before
                confidenceBar.fillAmount = c;

                // ALTERNATE OPTION
                // change the bar’s color: red if below threshold, green otherwise
                //const float threshold = 0.33f;
                //confidenceBar.color = (c < threshold)
                    //? Color.red
                    //: Color.green;

                // optional: interpolate smoothly instead of hard switch
                confidenceBar.color = Color.Lerp(Color.red, Color.green, c);

                // update the percentage text
                confidenceText.text = (c * 100f).ToString("F0") + "%";
            }
            else
            {
                nameText.text          = "<missing>";
                confidenceBar.fillAmount = 0f;
                confidenceBar.color    = Color.red;
                confidenceText.text    = "0%";
            }
        }
    }
}