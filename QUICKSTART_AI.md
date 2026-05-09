# Quick Start: AI Crossover with Learning

## 🚀 En 3 pasos:

### 1️⃣ Crea tu archivo de configuración
```bash
cd DarwinGA.Example
copy appsettings.Example.json appsettings.json
```

### 2️⃣ Agrega tu API key de OpenAI
Edita `DarwinGA.Example/appsettings.json`:
```json
{
  "AI": {
    "Provider": "ChatGPT",
    "ChatGPT": {
      "ApiKey": "sk-TU_CLAVE_AQUI",
      "Model": "gpt-3.5-turbo"
    }
  }
}
```

**¿Dónde consigo mi API key?**
👉 https://platform.openai.com/api-keys

### 3️⃣ Ejecuta el ejemplo
```bash
dotnet run --project DarwinGA.Example
```
Selecciona opción **5** del menú.

---

## 🧠 ¡NUEVO! La IA Aprende de Cada Generación

El sistema mantiene **historial de conversación**, permitiendo que la IA:

✅ **Recuerde** generaciones anteriores  
✅ **Aprenda** qué estrategias funcionan mejor  
✅ **Adapte** el crossover basándose en el progreso  
✅ **Mejore** la calidad de la descendencia con el tiempo  

---

## 📋 Uso en tu código

```csharp
// 1. Cargar configuración
var config = LoadConfiguration();

// 2. Crear proveedor de IA (con historial automático)
var aiProvider = new ChatGPTProvider(config.ApiKey, config.Model);

// 3. Crear crosser con función de fitness (para contexto)
var aiCrosser = new AICrosser(aiProvider)
{
    FitnessFunction = chromosome => CalculateFitness(chromosome)
};

// 4. Usar en el algoritmo genético
var ga = new GeneticAlgorithm<BinaryEvolutional>
{
    PopulationCrosser = aiCrosser,
    Cross = null, // No es necesario con PopulationCrosser
    Fitness = chromosome => CalculateFitness(chromosome),
    // ... resto de configuración
};

ga.Run(populationSize);

// 5. Ver estadísticas de aprendizaje
if (aiProvider is ChatGPTProvider chatGpt)
{
    Console.WriteLine($"Messages in history: {chatGpt.ConversationLength}");
}
```

---

## 🎯 Información Enviada a la IA

Cada generación incluye:
- 📊 Número de generación
- 🏆 Mejor fitness actual
- 📈 Fitness promedio
- 🧬 Población completa con fitness individual
- 💡 Contexto de generaciones anteriores

Esto permite que la IA tome decisiones **cada vez más inteligentes**.

---

## ⚠️ Importante

- ✅ El archivo `appsettings.json` está excluido de Git
- ✅ Nunca subas tu API key al repositorio
- ✅ Usa `appsettings.Example.json` como plantilla
- ⚠️ El historial aumenta el tamaño de las solicitudes (más contexto = más tokens)

---

## 💰 Costos aproximados

Con `gpt-3.5-turbo` y historial completo:
- 10 individuos x 5 generaciones = ~$0.005 USD
- 20 individuos x 10 generaciones = ~$0.02 USD

**Con historial, los costos crecen gradualmente:**
- Gen 1: ~200 tokens
- Gen 5: ~1000 tokens (incluye historial)
- Gen 10: ~2000 tokens

Monitorea tu uso en: https://platform.openai.com/usage

---

## 📚 Más información

- **Inicio rápido**: Este archivo (QUICKSTART_AI.md)
- **Arquitectura completa**: AI_CROSSOVER_ARCHITECTURE.md
- **Historial y aprendizaje**: AI_CONVERSATION_HISTORY.md ⭐ NUEVO
- **Configuración**: DarwinGA.Example/README_AI_CONFIG.md

