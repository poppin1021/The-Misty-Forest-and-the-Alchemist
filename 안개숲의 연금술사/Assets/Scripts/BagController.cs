using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class BagController : MonoBehaviour
{
    private string[] bagTokens =
    {
        "È«µî¿­¸Å",
        "È«µî¿­¸Å",
        "°©°¢ÀÙ",
        "°©°¢ÀÙ",
        "»õ»ì²É",
        "»õ»ì²É",
        "½Ãµê¹ö¼¸",
        "½Ãµê¹ö¼¸",
        "¹Ù¶÷¾¾¾Ñ",
        "¹Ù¶÷¾¾¾Ñ"
    };
    private List<string> handTokens = new List<string>();
    [SerializeField] private TMP_Text handText;

    public void DrawTokens()
    {
        handTokens.Clear();
        // Å×½ºÆ®¿ë ÁÖ¸Ó´Ï¸¦ ¿ø·¡ ±¸¼º´ë·Î »õ·Î ¸¸µç´Ù.
        List<string> remainingTokens = new List<string>(bagTokens);

        for (int i = 0; i < 5; i++)
        {
            // ÁÖ¸Ó´Ï°¡ ºñ¾ú´Ù¸é »Ì±â¸¦ ¸ØÃá´Ù.
            if (remainingTokens.Count == 0)
            {
                break;
            }

            int randomIndex = Random.Range(0, remainingTokens.Count);

            string drawnToken = remainingTokens[randomIndex];

            remainingTokens.RemoveAt(randomIndex);

            handTokens.Add(drawnToken);

            Debug.Log((i + 1) + "¹øÂ° ÅäÅ«: " + drawnToken);
        }

        Debug.Log("³²Àº ÅäÅ« °³¼ö: " + remainingTokens.Count);
        Debug.Log("²¨³½ Àç·á: " + string.Join(", ", handTokens));
        handText.text = string.Join(", ", handTokens);
    }
}