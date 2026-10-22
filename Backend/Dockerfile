FROM python:3.12-slim
WORKDIR /app
RUN useradd --create-home --uid 10001 rune && mkdir /data && chown rune:rune /data
COPY --chown=rune:rune server.py /app/server.py
USER rune
ENV PYTHONDONTWRITEBYTECODE=1 PYTHONUNBUFFERED=1 RB_DB=/data/runebastion.db RB_BIND=0.0.0.0 PORT=8088
EXPOSE 8088
HEALTHCHECK --interval=30s --timeout=3s CMD python -c "import urllib.request; urllib.request.urlopen('http://127.0.0.1:8088/health', timeout=2)"
CMD ["python", "server.py"]
