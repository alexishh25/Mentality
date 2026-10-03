from __future__ import annotations
import ollama
from models.Agent_dto import (
    EventoToqueObjeto,
    ComandoDuplicado,
    AIResponseSchema
)

class AIService:
    def __init__(self, model_name: str = "llama3"):
        self.model_name = model_name

    def process_touch_event(self, event: EventoToqueObjeto) -> ComandoDuplicado:
        # Prompt del sistema para guiar la lógica espacial de Llama 3
        system_prompt = (
            "Eres un motor de reglas e inteligencia espacial para un juego en Unity.\n"
            "Tu tarea es analizar el evento de toque de un jugador y determinar cuántos objetos duplicar "
            "y en qué coordenadas spawnearlos cerca del jugador.\n"
            "Reglas:\n"
            "1. La cantidad de objetos generados debe ser acorde al 'touch_count' registrado.\n"
            "2. Genera posiciones ligeramente dispersas (offset de 1.0 a 3.0 unidades) "
            "con respecto a 'player_position' para que no colisionen en el mismo punto."
        )

        user_prompt = (
            f"El jugador '{event.player_id}' tocó el objeto '{event.object_id}' "
            f"(Prefab: {event.prefab_name}) un total de {event.touch_count} veces.\n"
            f"Posición actual del jugador: x={event.player_position.x}, "
            f"y={event.player_position.y}, z={event.player_position.z}.\n"
            f"Calcula cuántos objetos generar y sus nuevas posiciones."
        )

        # Consulta a Ollama usando el esquema estructurado de Pydantic
        response = ollama.chat(
            model=self.model_name,
            messages=[
                {"role": "system", "content": system_prompt},
                {"role": "user", "content": user_prompt}
            ],
            format=AIResponseSchema.model_json_schema(), # Garantiza un JSON válido estricto
            options={"temperature": 0.3}
        )

        # Parsear la respuesta estructurada de la IA
        raw_json = response['message']['content']
        ai_data = AIResponseSchema.model_validate_json(raw_json)

        # Transformar el resultado al contrato exacto que espera Unity
        return ComandoDuplicado(
            action="SPAWN_TEMPORARY_OBJECTS",
            prefab_name=event.prefab_name,
            positions=ai_data.calculated_positions,
            duration_seconds=ai_data.duration_seconds
        )