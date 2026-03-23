# DarwinGA

Librería de algoritmos genéticos en .NET 8 con soporte para cromosomas binarios y redes neuronales basadas en `Accord.Neuro`. Incluye selección, cruce, mutación y criterios de terminación configurables, además de ejecución paralela opcional.

## Características

- Núcleo genérico `GeneticAlgorithm<TElement>`.
- Implementaciones de evolución:
  - `BinaryEvolutional` (cromosomas binarios).
  - `ActivationNetworkEvolutional` (redes neuronales `Accord.Neuro`).
- Operadores configurables:
  - Selección (`ISelection`), cruce (`ICross<T>`), mutación (`IMutation<T>`), terminación (`ITermination`).
- Ejecución paralela de evaluación y reproducción.
- Ejemplo completo en consola (OneMax).
- Tests con xUnit.

## Requisitos

- .NET 8 SDK

## Estructura del repositorio

- `DarwinGA/` → librería principal.
- `DarwinGA.Example/` → ejemplo de uso.
- `DarwinGA.Tests/` → tests.

## Uso rápido (OneMax)
