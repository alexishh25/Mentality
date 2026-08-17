
import json
import ollama
from models.Agent_dto import EventoToqueObjeto, DecisionDuplicado

SYSTEM_PROMPT = """
Eres un asistente de reglas físicas y lógica de juego en Unity.
Tu objetivo es analizar la posición actual y el evento de toque de un objeto, y decidir cuántas copias duplicadas de ese objeto deben instanciarse en la escena.

REGLAS:
- Responde ÚNICAMENTE en formato JSON válido que cumpla con el esquema requerido.
- La decisión debe basarse en la fuerza de impacto, el tiempo de interacción y la ubicación espacial.
- `cantidad_duplicar` debe ser un entero entre 0 y 5.
"""

def decidir_duplicacion_llama(evento: EventoToqueObjeto) -> DecisionDuplicado:
    payload = {
        "objeto": evento.nombre_objeto,
        "posicion": evento.posicion.model_dump(),
        "fuerza_impacto": evento.fuerza_impacto,
        "tiempo_interaccion": evento.tiempo_interaccion_seg
    }

    response = ollama.chat(
        model="llama3",
        messages=[
            {"role": "system", "content": SYSTEM_PROMPT},
            {"role": "user", "content": f"Analiza este evento de toque: {json.dumps(payload)}"}
        ],
        format="json", 
        options={"temperature": 0.2}
    )

    content = response['message']['content']
    data = json.loads(content)
    
    return DecisionDuplicado(**data)