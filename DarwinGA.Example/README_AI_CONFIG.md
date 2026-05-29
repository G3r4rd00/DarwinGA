# Configuración de IA (Inicio de Población + Crossover)

## Lo más destacado

Ahora el ejemplo permite activar **dos capacidades clave** de forma independiente:

1. **Inicio de población por IA**
   - La IA genera la población inicial en lugar de crearla de forma aleatoria.
2. **Crossover por IA**
   - La IA propone la nueva población en cada generación usando el estado actual.

Al ejecutar `DarwinGA.Example`, verás estas preguntas:

```text
Use AI for initial population creation? (y/N):
Use AI for crossover? (y/N):
```

Puedes usar una sola opción o ambas al mismo tiempo.

---

## Instalación por NuGet

Si todavía no tienes la librería instalada en tu proyecto:

```bash
dotnet add package DarwinGA
```

O desde la consola de NuGet en Visual Studio:

```powershell
Install-Package DarwinGA
```

---

## Configuración rápida

Edita `DarwinGA.Example/appsettings.json` con tus valores:

```json
{
  "GeneticAlgorithm": {
    "PopulationSize": 50,
    "MutationProbability": 0.2
  },
  "OpenAI": {
    "ApiKey": ""
  },
  "DeepSeek": {
    "ApiKey": "",
    "BaseUrl": "https://api.deepseek.com"
  },
  "LMStudio": {
    "BaseUrl": "http://localhost:1234/v1"
  }
}
```

También puedes pasar claves por variables de entorno:
- `OPENAI_API_KEY`
- `DEEPSEEK_API_KEY`

---

## Proveedores soportados

Durante la ejecución puedes elegir:

1. **OpenAI**
2. **DeepSeek**
3. **LM Studio** (API local compatible con OpenAI)

### OpenAI
- Requiere `OpenAI:ApiKey` o `OPENAI_API_KEY`.
- Modelos disponibles en el menú del ejemplo (incluye `gpt-4o`, `gpt-4`, `gpt-3.5-turbo`, etc.).

### DeepSeek
- Requiere `DeepSeek:ApiKey` o `DEEPSEEK_API_KEY`.
- `DeepSeek:BaseUrl` por defecto: `https://api.deepseek.com`.

### LM Studio
- Ideal para uso local sin depender de nube.
- `LMStudio:BaseUrl` por defecto: `http://localhost:1234/v1`.
- El ejemplo intenta descubrir modelos automáticamente desde `/models`.

---

## ¿Qué hace cada parámetro?

- `GeneticAlgorithm:PopulationSize`: tamaño de la población.
- `GeneticAlgorithm:MutationProbability`: probabilidad de mutación (0.0 a 1.0).

---

## Seguridad

- No publiques tus API keys en el repositorio.
- Usa `appsettings.json` local para secretos.
- Si compartes ejemplos, elimina o enmascara credenciales.

---

## Coste y rendimiento

Cuando usas IA (inicio de población y/o crossover), hay llamadas al proveedor en tiempo de ejecución.

Recomendaciones:
- Empieza con poblaciones pequeñas (10-20).
- Usa pocas generaciones para pruebas iniciales.
- Monitorea consumo si usas proveedores cloud (OpenAI/DeepSeek).
