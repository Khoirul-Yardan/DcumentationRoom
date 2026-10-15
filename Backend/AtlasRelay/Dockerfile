FROM python:3.12-slim
WORKDIR /app
COPY server.py /app/server.py
USER 10001:10001
EXPOSE 47778
ENTRYPOINT ["python", "-u", "/app/server.py"]
CMD ["--host", "0.0.0.0", "--port", "47778", "--cert", "/certs/fullchain.pem", "--key", "/certs/privkey.pem"]
