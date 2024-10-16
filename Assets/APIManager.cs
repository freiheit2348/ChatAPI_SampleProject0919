using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Text;
using System.IO;

public class APIManager : MonoBehaviour
{
    public InputField inputField;
    public Button sendButton;
    public string serverUrl = "http://34.31.191.110:5000"; // GCEインスタンスの外部IPアドレス
    private AudioSource audioSource; // 音声再生用

    void Start()
    {
        sendButton.onClick.AddListener(SendRequest);
        audioSource = gameObject.AddComponent<AudioSource>(); // AudioSourceコンポーネントを追加
        Debug.Log("APIManager initialized. Server URL: " + serverUrl);
    }

    void SendRequest()
    {
        Debug.Log("Send request initiated with input: " + inputField.text);
        StartCoroutine(SendChatRequest(inputField.text));
    }

    IEnumerator SendChatRequest(string input)
    {
        string jsonPayload = JsonUtility.ToJson(new ChatRequest { user_input = input });
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        Debug.Log("Sending chat request to: " + serverUrl + "/chat");
        using (UnityWebRequest www = new UnityWebRequest(serverUrl + "/chat", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.timeout = 120; // タイムアウトを120秒に設定

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string response = www.downloadHandler.text;
                Debug.Log("Chat response received: " + response);
                ChatResponse chatResponse = JsonUtility.FromJson<ChatResponse>(response);
                StartCoroutine(GenerateAudio(chatResponse.ai_response));
            }
            else
            {
                Debug.LogError("Error in chat request: " + www.error);
            }
        }
        Debug.Log("Chat request completed, starting audio generation");
    }

    IEnumerator GenerateAudio(string text)
    {
        string jsonPayload = JsonUtility.ToJson(new AudioRequest { text = text });
        byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);
        Debug.Log("Sending audio generation request to: " + serverUrl + "/generate_audio");
        using (UnityWebRequest www = new UnityWebRequest(serverUrl + "/generate_audio", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");
            www.timeout = 60; // タイムアウトを60秒に設定

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string directory = Application.persistentDataPath;
                string fileName = "response.mp3";
                string path = Path.Combine(directory, fileName);
                File.WriteAllBytes(path, www.downloadHandler.data);
                Debug.Log("Audio file saved at: " + path);
                StartCoroutine(PlayAudioFromFile(path));
            }
            else
            {
                Debug.LogError("Error in audio generation: " + www.error);
            }
        }
    }

    IEnumerator PlayAudioFromFile(string filePath)
    {
        Debug.Log("Attempting to load audio from: " + filePath);
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + filePath, AudioType.MPEG))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                if (clip != null)
                {
                    audioSource.clip = clip;
                    audioSource.Play();
                    Debug.Log("Audio playback started");
                }
                else
                {
                    Debug.LogError("Failed to load AudioClip");
                }
            }
            else
            {
                Debug.LogError("Error loading audio: " + www.error);
            }
        }
    }

    [System.Serializable]
    private class ChatRequest
    {
        public string user_input;
    }

    [System.Serializable]
    private class ChatResponse
    {
        public string ai_response;
    }

    [System.Serializable]
    private class AudioRequest
    {
        public string text;
    }
}