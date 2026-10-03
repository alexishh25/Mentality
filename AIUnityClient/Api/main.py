import subprocess
import time
from contextlib import asynccontextmanager
import requests
from fastapi import FastAPI


from routers.logs_router import router as logs_router
from routers.health_router import router as health_router
from routers.interaction_router import router as interaction_router

MODEL_NAME = "llama3"
OLLAMA_URL = "http://localhost:11434/api/generate"

def ensure_ollama_running():
    """Comprueba si el proceso de Ollama está activo; si no, lo inicia."""
    try:
        requests.get("http://localhost:11434/", timeout=2)
        print("🟢 Servicio de Ollama detectado y activo.")
    except (requests.exceptions.ConnectionError, requests.exceptions.Timeout):
        print("⚠️ Ollama no responde. Iniciando servicio 'ollama serve'...")
        subprocess.Popen(
            ["ollama", "serve"],
            stdout=subprocess.DEVNULL,
            stderr=subprocess.DEVNULL
        )
        time.sleep(3)

def preload_llama_model():
    """Carga Llama 3 en memoria RAM/VRAM al arrancar la API para evitar retrasos."""
    print(f"🔄 Precargando modelo '{MODEL_NAME}'...")
    try:
        requests.post(
            OLLAMA_URL,
            json={"model": MODEL_NAME, "keep_alive": -1},  # -1 mantiene el modelo cargado indefinidamente
            timeout=30
        )
        print(f"✅ Modelo '{MODEL_NAME}' listo para procesar peticiones.")
    except Exception as e:
        print(f"❌ Error al precargar Llama 3: {e}")

@asynccontextmanager
async def lifespan(app: FastAPI):
    # --- Evento al iniciar la API ---
    ensure_ollama_running()
    preload_llama_model()
    
    yield
    
    # --- Evento al apagar la API ---
    print("🔴 Apagando el servidor API...")

app = FastAPI(title="AI Backend", lifespan=lifespan)

app.include_router(logs_router)
app.include_router(health_router)
app.include_router(interaction_router)