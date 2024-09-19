import requests
from playsound import playsound

BASE_URL = "http://localhost:5000"

while True:
    user_input = input("あなた: ")
    if user_input.lower() == 'quit':
        break

    try:
        # チャットリクエスト
        response = requests.post(f"{BASE_URL}/chat", json={"user_input": user_input})
        response.raise_for_status()  # HTTPエラーがあれば例外を発生させる
        print("Response status:", response.status_code)
        print("Response content:", response.text)
        ai_response = response.json()["ai_response"]
        print("AI:", ai_response)

        # 音声生成リクエスト
        audio_response = requests.post(f"{BASE_URL}/generate_audio", json={"text": ai_response})
        audio_response.raise_for_status()
        if audio_response.status_code == 200:
            with open("response.mp3", "wb") as f:
                f.write(audio_response.content)
            print("音声を再生します...")
            playsound("response.mp3")
        else:
            print("音声の生成に失敗しました")

    except requests.exceptions.RequestException as e:
        print(f"リクエストエラー: {e}")
    except ValueError as e:
        print(f"JSONデコードエラー: {e}")
        print(f"レスポンス内容: {response.text}")
    except KeyError as e:
        print(f"キーエラー: {e}")
        print(f"レスポンス内容: {response.json()}")
    except Exception as e:
        print(f"予期せぬエラー: {e}")

print("プログラムを終了します")