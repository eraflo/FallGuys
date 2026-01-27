using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using FallGuys.Networking;

public class LobbyPlayerCard : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _playerNameText;
    [SerializeField] private Image _readyStatusImage; // Background or Icon
    [SerializeField] private Image _avatarImage;      // Optional avatar icon

    [Header("Visual Settings")]
    [SerializeField] private Color _notReadyColor = new Color(1f, 1f, 1f, 0.2f);
    [SerializeField] private Color _readyColor = new Color(0.5f, 1f, 0.5f, 1f);
    
    private ulong _clientId;

    public void Initialize(PlayerData data)
    {
        _clientId = data.ClientId;
        UpdateDisplay(data);
        StartCoroutine(AnimateEntry());
    }

    public void UpdateDisplay(PlayerData data)
    {
        if (_playerNameText != null)
        {
            // Simple check to display "YOU" for local player could be added here if we passed LocalClientId
            _playerNameText.text = data.PlayerName.ToString(); 
        }

        if (_readyStatusImage != null)
        {
            Color targetColor = data.IsReady ? _readyColor : _notReadyColor;
            // Simple Lerp for color could go here, but instant is fine for now
            _readyStatusImage.color = targetColor;
        }

        if (data.IsReady)
        {
            StartCoroutine(PunchScale(Vector3.one * 1.1f, 0.2f));
        }
    }

    private IEnumerator AnimateEntry()
    {
        transform.localScale = Vector3.zero;
        float duration = 0.3f;
        float time = 0f;

        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            // Elastic ease out
            float scale = Mathf.Sin(-13f * (t + 1f) * Mathf.PI * 0.5f) * Mathf.Pow(2f, -10f * t) + 1f;
            transform.localScale = Vector3.one * scale;
            yield return null;
        }
        transform.localScale = Vector3.one;
    }

    private IEnumerator PunchScale(Vector3 punchAmount, float duration)
    {
        Vector3 originalScale = Vector3.one;
        Vector3 targetScale = punchAmount;
        
        float halfDuration = duration / 2f;
        float time = 0f;

        // Scale Up
        while (time < halfDuration)
        {
            time += Time.deltaTime;
            transform.localScale = Vector3.Lerp(originalScale, targetScale, time / halfDuration);
            yield return null;
        }

        time = 0f;
        // Scale Down
        while (time < halfDuration)
        {
            time += Time.deltaTime;
            transform.localScale = Vector3.Lerp(targetScale, originalScale, time / halfDuration);
            yield return null;
        }
        transform.localScale = originalScale;
    }
}
