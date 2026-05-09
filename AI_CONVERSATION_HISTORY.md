# AI Crossover with Conversation History

## 🎯 Nueva Característica: Historial de Conversación

El proveedor de ChatGPT ahora **mantiene el historial completo de la conversación**, lo que permite que la IA:

1. **Aprenda de generaciones anteriores**
2. **Entienda la progresión evolutiva**
3. **Tome decisiones más informadas sobre crossover**
4. **Mejore la calidad de la descendencia con el tiempo**

---

## 🧠 Cómo Funciona

### Sin Historial (Antes)
```
Generación 1: "Cruza esta población" → IA responde
Generación 2: "Cruza esta población" → IA responde (sin contexto previo)
Generación 3: "Cruza esta población" → IA responde (sin contexto previo)
```

### Con Historial (Ahora)
```
Generación 1: "Gen 1, Best: 10, Avg: 8, Cruza..." → IA responde
Generación 2: "Gen 2, Best: 12, Avg: 9, Cruza..." → IA ve mejora y adapta
Generación 3: "Gen 3, Best: 14, Avg: 10, Cruza..." → IA continúa aprendiendo
```

---

## 📊 Información de Contexto Enviada

Cada generación incluye:

```
Generation 3:

Current population statistics:
- Best fitness: 14.00
- Average fitness: 10.50

Perform crossover to create offspring that:
1. Preserve genetic material from high-fitness parents
2. Explore new combinations that might improve fitness
3. Maintain diversity in the population

Input population (with fitness scores):
[
  {
    "chromosome": "11001101011001101101",
    "fitness": 14.0
  },
  {
    "chromosome": "10101010101010101010",
    "fitness": 10.0
  },
  ...
]
```

---

## 🔧 Características del ChatGPTProvider

### Propiedades

- **`ConversationLength`**: Número de mensajes en el historial
- **System Message**: Prompt inicial optimizado para evolución

### Métodos

- **`SendPromptAsync(string)`**: Envía un prompt y mantiene el historial
- **`ResetConversation()`**: Limpia el historial (mantiene system message)

### Ejemplo de Uso

```csharp
var provider = new ChatGPTProvider(apiKey, "gpt-3.5-turbo");

// Primera generación
var response1 = await provider.SendPromptAsync("Generation 1...");

// Segunda generación (con contexto de la primera)
var response2 = await provider.SendPromptAsync("Generation 2...");

// El provider recuerda ambas conversaciones
Console.WriteLine($"Messages: {provider.ConversationLength}"); // 5
// (1 system + 2 user + 2 assistant)

// Opcional: reiniciar para una nueva ejecución
provider.ResetConversation();
```

---

## 🎯 Características del AICrosser

### Nueva Propiedad: `FitnessFunction`

```csharp
var aiCrosser = new AICrosser(aiProvider)
{
    FitnessFunction = chromosome => CalculateFitness(chromosome)
};
```

Cuando se configura:
- ✅ Envía fitness de cada individuo a la IA
- ✅ Incluye estadísticas (mejor, promedio)
- ✅ Proporciona contexto para mejores decisiones
- ✅ Permite a la IA identificar patrones de éxito

### Seguimiento Interno

- **`_generationCount`**: Número de generación actual
- **`_lastBestFitness`**: Mejor fitness de la generación anterior
- **`_lastAverageFitness`**: Fitness promedio de la generación anterior

---

## 💡 Ventajas del Historial

### 1. Aprendizaje Progresivo
La IA ve cómo evolucionan las soluciones y puede:
- Identificar qué estrategias de crossover funcionan mejor
- Ajustar su enfoque basándose en el progreso
- Mantener o cambiar tácticas según los resultados

### 2. Contexto Evolutivo
La IA entiende:
- Si la población está mejorando o estancada
- Qué características genéticas están teniendo éxito
- Cuándo necesita explorar vs. explotar

### 3. Calidad Mejorada
Con más contexto, la IA puede:
- Preservar mejor las características exitosas
- Hacer cruces más inteligentes
- Mantener diversidad cuando es necesario

---

## ⚙️ Configuración Recomendada

### Para Experimentación
```csharp
var provider = new ChatGPTProvider(apiKey, "gpt-4");
var aiCrosser = new AICrosser(provider)
{
    FitnessFunction = fitnessFunc // Incluir fitness para mejor contexto
};

// Pocas generaciones para ver el aprendizaje
Termination = new GenerationNumTermination(10)
```

### Para Producción
```csharp
var provider = new ChatGPTProvider(apiKey, "gpt-3.5-turbo");
var aiCrosser = new AICrosser(provider)
{
    FitnessFunction = fitnessFunc
};

// Más generaciones con costo controlado
Termination = new GenerationNumTermination(20)
```

---

## 📈 Ejemplo de Salida

```
Starting evolution with AI crossover...
(This will make API calls to OpenAI)

Gen: 0    | Best:  11.0/20 | Avg:   9.20 | Ones: 11/20
Gen: 1    | Best:  13.0/20 | Avg:  10.50 | Ones: 13/20
Gen: 2    | Best:  15.0/20 | Avg:  12.10 | Ones: 15/20
Gen: 3    | Best:  17.0/20 | Avg:  14.30 | Ones: 17/20
Gen: 4    | Best:  18.0/20 | Avg:  15.80 | Ones: 18/20

=== Final Best Solution ===
Chromosome: 11111111111111111100
Fitness: 18/20

=== AI Conversation Statistics ===
Total messages in history: 11
(The AI learned from each generation to improve crossover quality)
```

Nota cómo el fitness promedio también mejora, sugiriendo que la IA está aprendiendo a hacer mejores cruces.

---

## 🔍 Debugging y Monitoreo

### Ver tamaño del historial
```csharp
if (aiProvider is ChatGPTProvider chatGpt)
{
    Console.WriteLine($"Conversation length: {chatGpt.ConversationLength}");
}
```

### Reiniciar entre ejecuciones
```csharp
// Si ejecutas múltiples GAs con el mismo provider
if (aiProvider is ChatGPTProvider chatGpt)
{
    chatGpt.ResetConversation();
}
```

---

## 💰 Consideraciones de Costo

⚠️ **El historial aumenta el tamaño de las solicitudes**

- Generación 1: ~200 tokens
- Generación 5: ~1000 tokens (incluye historial)
- Generación 10: ~2000 tokens (historial completo)

**Estrategias de optimización:**

1. **Limitar generaciones**: Usa `GenerationNumTermination` moderado
2. **Población pequeña**: 10-20 individuos para demos
3. **Reiniciar periódicamente**: Llama `ResetConversation()` cada N generaciones
4. **Usar gpt-3.5-turbo**: Más económico para desarrollo

---

## 🎓 System Prompt Mejorado

El provider inicializa con un prompt optimizado:

```
You are a genetic algorithm assistant specialized in evolutionary computation. 
You receive populations of binary chromosomes in JSON format and perform 
crossover operations to create offspring.

Your goal is to apply intelligent crossover strategies that:
1. Preserve good genetic material from fit parents
2. Explore new combinations that might improve fitness
3. Maintain population diversity
4. Learn from previous generations to improve crossover quality

Always respond with ONLY a valid JSON array of binary strings.
Learn from the evolutionary progress across generations to make 
better crossover decisions.
```

Este prompt guía a la IA para que use el contexto histórico efectivamente.

---

## 🚀 Próximos Pasos

Ideas para mejorar aún más:

- [ ] Implementar ventana deslizante de historial (últimas N generaciones)
- [ ] Agregar métricas de diversidad al contexto
- [ ] Incluir información sobre estancamiento
- [ ] Implementar estrategias adaptativas basadas en respuestas de IA
- [ ] Logging estructurado de conversaciones
- [ ] Análisis de qué patrones de crossover usa la IA

---

## 📚 Referencias

- [OpenAI Chat Completions API](https://platform.openai.com/docs/guides/chat)
- [Token Counting](https://platform.openai.com/tokenizer)
- [Best Practices for Prompting](https://platform.openai.com/docs/guides/prompt-engineering)
