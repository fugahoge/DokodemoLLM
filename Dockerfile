# Ollama + gemma3:1b モデルをビルド時に取得
FROM ollama/ollama:latest

# ヘルスチェック用にcurlをインストール
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

# Ollamaバージョンを表示
RUN echo "Ollama version:" && /bin/ollama --version

# ビルド時にOllamaサーバーを起動してモデルを取得
# （RUN時はサーバーが動いていないため、起動→待機→pull→停止の流れが必要）
RUN ollama serve & \
    serve_pid=$! && \
    echo "Ollamaサーバー起動中..." && \
    for i in $(seq 1 30); do \
        if curl -s http://localhost:11434 >/dev/null 2>&1; then \
            echo "Ollamaサーバー準備完了"; \
            break; \
        fi; \
        if [ $i -eq 30 ]; then \
            echo "タイムアウト: Ollamaサーバーの起動に失敗"; \
            kill $serve_pid 2>/dev/null || true; \
            exit 1; \
        fi; \
        sleep 2; \
    done && \
    echo "gemma3:1b モデルを取得中..." && \
    ollama pull gemma3:1b && \
    echo "モデル取得完了" && \
    kill $serve_pid 2>/dev/null || true && \
    wait $serve_pid 2>/dev/null || true

EXPOSE 11434
ENV OLLAMA_HOST=0.0.0.0:11434

ENTRYPOINT ["/bin/ollama"]
CMD ["serve"]
