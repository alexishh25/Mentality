import os
import sys
import uvicorn

# Añade la carpeta 'Api' al camino de búsqueda de Python para que encuentre los 'routers'
sys.path.append(os.path.join(os.path.dirname(__file__), "Api"))

if __name__ == "__main__":
    uvicorn.run(
        "main:app", 
        host="127.0.0.1",
        port=8000
    )