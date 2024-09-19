import io
import os
from flask import Flask, request, jsonify, send_file
from openai import OpenAI
from elevenlabs import generate, save
from dotenv import load_dotenv

# 環境変数をロード
load_dotenv()

app = Flask(__name__)

# 環境変数からAPIキーを取得
OPENAI_API_KEY = os.getenv("OPENAI_API_KEY")
ELEVENLABS_API_KEY = os.getenv("ELEVENLABS_API_KEY")

openai_client = OpenAI(api_key=OPENAI_API_KEY)

SYSTEM_PROMPT = "あなたはAIアシスタントのワタナベです。機転を聞かせ、効率的に対応してください。すべての応答を完全にひらがなとカタカナのみで行ってください。漢字は一切使用しないでください。"

@app.route('/chat', methods=['POST'])
def chat():
    user_input = request.json.get('user_input', '')
    response = openai_client.chat.completions.create(
        model="gpt-3.5-turbo",
        messages=[
            {"role": "system", "content": SYSTEM_PROMPT},
            {"role": "user", "content": user_input}
        ]
    )
    ai_response = response.choices[0].message.content
    return jsonify({"ai_response": ai_response})

@app.route('/generate_audio', methods=['POST'])
def generate_audio():
    text = request.json.get('text', '')
    audio_stream = generate(
        api_key=ELEVENLABS_API_KEY,
        text=text,
        voice="6dL8PSUsZhxlmUrCBDKb",
        model="eleven_multilingual_v2",
        stream=True
    )
    
    # ストリームからバイトデータを作成
    audio_data = io.BytesIO()
    for chunk in audio_stream:
        if chunk:  # チャンクが空でないことを確認
            audio_data.write(chunk)
    audio_data.seek(0)
    
    return send_file(audio_data, mimetype="audio/mpeg", as_attachment=True, download_name="response.mp3")

if __name__ == '__main__':
    app.run(debug=True)