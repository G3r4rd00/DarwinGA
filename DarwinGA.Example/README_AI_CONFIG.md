# Configuración de IA (Inicio de Población + Crossover)

## Lo más destacado

Ahora el ejemplo permite activar **dos capacidades clave** de forma independiente:

1. **Inicio de población por IA**
   - La IA genera la población inicial en lugar de crearla de forma aleatoria.
2. **Crossover por IA**
   - La IA propone la nueva población en cada generación usando el estado actual.

Al ejecutar `DarwinGA.Example`, verás estas preguntas:

```text
Use AI for initial population creation? (Y/n):
Use AI for crossover? (Y/n):
```

Puedes usar una sola opción o ambas al mismo tiempo. Ambas están activadas por defecto (pulsa Enter); responde `n` para desactivarlas.

Por defecto el proveedor es **Claude** con el modelo **opus**.

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
    "MutationProbability": 0.1,
    "AIInitialPopulationCount": 3,
    "AICrossoverRatio": 0.1
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
  },
  "Claude": {
    "Model": "opus",
    "CliPath": "",
    "TimeoutSeconds": 600
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
4. **Claude** (vía Claude Code CLI, usando tu suscripción de Claude)

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

### Claude (suscripción)
- No necesita API key: ejecuta `claude -p` (Claude Code en modo headless) con la cuenta con la que hayas iniciado sesión (Pro/Max).
- Requisitos: tener Claude Code instalado (`npm install -g @anthropic-ai/claude-code`) y haber iniciado sesión ejecutando `claude` una vez.
- Modelos en el menú: `sonnet`, `opus`, `haiku`, `fable` (alias de la última versión de cada uno) u **Other** para escribir un nombre completo (p. ej. `claude-opus-5-5`).
- `Claude:Model`: modelo usado si pulsas Enter sin elegir opción (por defecto `opus`).
- `Claude:CliPath`: ruta al ejecutable `claude` (vacío = se busca en el PATH).
- `Claude:TimeoutSeconds`: tiempo máximo por respuesta (por defecto 600).
- Se ignora `ANTHROPIC_API_KEY` para forzar el uso de la suscripción. El consumo cuenta contra los límites de uso de tu plan.

---

## ¿Qué hace cada parámetro?

- `GeneticAlgorithm:PopulationSize`: tamaño de la población.
- `GeneticAlgorithm:MutationProbability`: probabilidad de mutación (0.0 a 1.0).
- `GeneticAlgorithm:MaxGenerations`: máximo de generaciones (por defecto 100); también se para al encontrar la ruta óptima.
- `GeneticAlgorithm:AIInitialPopulationCount`: individuos de la población inicial generados por IA (por defecto 3); el resto son aleatorios.
- `GeneticAlgorithm:AICrossoverRatio`: fracción de cada generación producida por el crossover IA (por defecto 0.1 = 10%); el resto usa el crossover clásico.

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
