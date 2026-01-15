# Sistema de Combate

## Resumen

El sistema de combate es dinámico y soporta múltiples estilos de juego. Utiliza un sistema de targeting híbrido inspirado en MMORPGs clásicos y Argentum Online.

- **Selección de Habilidad**: Teclas 1-6.
- **Ejecución**:
  - **Single Target**: Selecciona objetivo + click izquierdo.
  - **AoE (Área)**: Click en objetivo o click en el suelo (Ground Targeting).
- **Casting**: Soporta habilidades instantáneas, con tiempo de casteo y canalizadas. El movimiento cancela el casteo de habilidades estáticas.

---

## Lógica de Combate Implementada

### 1. Sistema de Targeting (Modo Argentum)
1. El jugador presiona una tecla de habilidad (1-6).
2. Si la habilidad requiere objetivo o es AoE, aparece el **Cursor Cruz**.
3. **Click en Entidad**: Ejecuta la habilidad sobre ese objetivo.
4. **Click en Suelo**: Si la habilidad es AoE (Meteoro, Tormenta de Fuego), se ejecuta en esa posición exacta.
5. **Cancelación**: Click derecho o ESC cancelan la selección.

### 2. Estados de Habilidad y Casting
| Tipo de Casting | Comportamiento |
|-----------------|----------------|
| **Instant** | Se dispara inmediatamente. |
| **Casting** | Requiere quedarse quieto durante X segundos. El movimiento cancela el casteo. |
| **Channel** | Efecto continuo mientras se mantiene el casteo. |
| **Movement** | Permite castear mientras el jugador se desplaza. |

---

## Tipos de Habilidad (AbilityType)

- **Damage**: Aplica daño directo (Single/AoE).
- **Heal**: Restaura vida al objetivo o aliados en área.
- **Buff**: Aplica efectos positivos. Incluye **Sistema de Escudos** (absorción de daño).
- **Debuff**: Aplica efectos negativos. Incluye **Sistema de Ralentización (Slow)**.

---

## Mecánicas Avanzadas

### Área de Efecto (AoE)
- **Detección**: Usa `Physics.OverlapSphere` en el servidor para detectar múltiples objetivos.
- **Sin Autodaño**: El sistema evita que el caster se haga daño a sí mismo con sus propias áreas.
- **Ground Casting**: Permite apuntar a una posición del mundo sin necesidad de designar a un enemigo.

### Escudos (Barrera de Hielo)
- Implementado en `PlayerStats`. El daño entrante se resta primero del valor del escudo (`shieldAmount`) antes de afectar la vida del jugador.

### Ralentización (Slow)
- Implementado en `PlayerController`. Reduce la velocidad de movimiento del objetivo en un 50% durante una duración determinada (ej. Rayo Helado).

---

## Habilidades por Clase (Estado Actual)

### Guerrero (Melee - 2m)
| Nombre | Tipo | AoE | Notas |
|--------|------|-----|-------|
| Ataque Básico | Damage | No | Instantáneo. |
| Golpe Pesado | Damage | No | Gran daño. |
| Tajo Giratorio | Damage | **Sí** | Ahora daña a todos los enemigos cercanos. |

### Mago (Rango - 15m)
| Nombre | Tipo | AoE | Notas |
|--------|------|-----|-------|
| Bola de Fuego | Damage | No | Daño base alto. |
| Rayo Helado | Debuff | No | Aplica daño y **Ralentización (50%)**. |
| Meteoro | Damage | **Sí** | Gran daño en área. Soporta **Ground Targeting**. |
| Tormenta de Fuego| Damage | **Sí** | Área persistente de fuego. |
| Barrera de Hielo | Buff | No | Aplica un **Escudo de 50 pts** de absorción. |

### Cazador (Rango - 15m)
| Nombre | Tipo | AoE | Notas |
|--------|------|-----|-------|
| Flecha | Damage | No | Disparo básico. |
| Lluvia de Flechas | Damage | **Sí** | Daño masivo en área seleccionada. |
| Disparo Certero | Damage | No | Daño crítico. |

### Sacerdote (Rango - 15m)
| Nombre | Tipo | AoE | Notas |
|--------|------|-----|-------|
| Curación | Heal | No | Restaura vida a un aliado. |
| Bendición | Heal | **Sí** | Curación masiva en área. |
| Castigo Divino | Damage | No | Daño sagrado instantáneo. |

---

## Archivos Clave del Sistema

- `AbilityData.cs`: Definición de datos (CastingType, AoERadius, BaseDamage).
- `PlayerCombat.cs`: El "cerebro". Maneja cooldowns, estados de casteo y lógica de efectos.
- `PlayerStats.cs`: Gestiona HP, Maná y el sistema de **Escudos**.
- `TargetingSystem.cs`: Maneja el raycast, la selección de objetivos y el **Ground targeting**.
- `EnemyController.cs`: IA básica que utiliza el sistema de daño unificado.

---

## Notas de Desarrollo
- **Server Authority**: Todas las validaciones de rango, maná y daño ocurren en el servidor.
- **SyncVars**: Los estados de casteo y escudos están sincronizados automáticamente vía Mirror.
- **Fácil Expansión**: Para crear una habilidad nueva, solo crea el Asset en `Resources/Abilities/` y el sistema lo reconocerá automáticamente según su `AbilityType`.
