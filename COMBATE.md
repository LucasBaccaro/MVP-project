# Sistema de Combate

## Resumen

El sistema de combate tiene dos modos diferenciados según el tipo de clase:

- **Melee (Guerrero)**: Selecciona target + presiona tecla = ejecuta habilidad
- **Rango (Mago, Cazador, Sacerdote)**: Presiona tecla = aparece cursor cruz + click en objetivo = ejecuta habilidad (estilo Argentum Online)

---

## Lógica de Combate Implementada

### Flujo Melee (range < 3m)
```
1. Jugador selecciona objetivo (click izquierdo o Tab)
2. Jugador presiona tecla 1-6
3. Sistema valida: cooldown, maná, rango, línea de visión, zona segura
4. Si válido → ejecuta habilidad inmediatamente
5. Aplica daño/heal al objetivo
```

### Flujo Rango - Estilo Argentum (range >= 3m)
```
1. Jugador presiona tecla 1-6
2. Aparece cursor CRUZ en el mundo (rojo = sin objetivo, verde = objetivo válido)
3. Jugador mueve el mouse para apuntar
4. Click izquierdo en objetivo → ejecuta habilidad
5. Click derecho / ESC / click en vacío → cancela selección
```

### Archivos Clave
| Archivo | Responsabilidad |
|---------|-----------------|
| `PlayerCombat.cs` | Manejo de input, validaciones, ejecución de habilidades |
| `TargetingSystem.cs` | Selección de objetivos, cursor cruz, raycast |
| `AbilityData.cs` | ScriptableObject con datos de cada habilidad |
| `AbilityDatabase.cs` | Singleton que carga todas las habilidades |

---

## Clases y Estadísticas

| Clase | HP | Maná | Daño | HP Regen | Mana Regen | Tipo Combate |
|-------|-----|------|------|----------|------------|--------------|
| **Guerrero** | 150 | 30 | 15 | 2/s | 1/s | Melee |
| **Mago** | 80 | 150 | 8 | 1/s | 5/s | Rango |
| **Cazador** | 100 | 80 | 12 | 1.5/s | 2/s | Rango |
| **Sacerdote** | 110 | 120 | 7 | 3/s | 4/s | Rango |

---

## Habilidades por Clase

### Guerrero (Melee - Range 2m)

| ID | Nombre | Daño | Maná | CD | Tipo | Estado |
|----|--------|------|------|-----|------|--------|
| 0 | Ataque Básico | 10 | 0 | 0s | Damage | ✅ Funciona |
| 1 | Golpe Pesado | 25 | 10 | 5s | Damage | ✅ Funciona |
| 8 | Escudo Protector | 0 | 15 | 10s | Buff | ❌ Sin lógica |
| 9 | Carga | 20 | 10 | 8s | Damage | ⚠️ Solo daño (no dash) |
| 10 | Grito de Guerra | 0 | 20 | 15s | Buff | ❌ Sin lógica |
| 11 | Tajo Giratorio | 30 | 15 | 6s | Damage | ⚠️ Solo single target (debería ser AoE) |

### Mago (Rango 15m)

| ID | Nombre | Daño | Maná | CD | Tipo | Estado |
|----|--------|------|------|-----|------|--------|
| 2 | Bola de Fuego | 30 | 20 | 3s | Damage | ✅ Funciona |
| 3 | Rayo Helado | 20 | 15 | 2s | Damage | ✅ Funciona |
| 12 | Tormenta de Fuego | 45 | 35 | 10s | Damage | ⚠️ Solo single target (debería ser AoE) |
| 13 | Barrera de Hielo | 0 | 25 | 12s | Buff | ❌ Sin lógica |
| 14 | Explosión Arcana | 25 | 18 | 4s | Damage | ✅ Funciona |
| 15 | Meteoro | 80 | 50 | 20s | Damage | ⚠️ Solo single target (debería ser AoE) |

### Cazador (Rango 15m)

| ID | Nombre | Daño | Maná | CD | Tipo | Estado |
|----|--------|------|------|-----|------|--------|
| 4 | Flecha | 15 | 5 | 1s | Damage | ✅ Funciona |
| 5 | Disparo Múltiple | 35 | 25 | 8s | Damage | ⚠️ Solo single target (debería ser multi-hit) |
| 16 | Trampa | 25 | 15 | 12s | Damage | ⚠️ Solo daño (debería colocarse en suelo) |
| 17 | Flecha Venenosa | 20 | 12 | 5s | Damage | ⚠️ Solo daño instantáneo (debería ser DoT) |
| 18 | Disparo Certero | 40 | 20 | 8s | Damage | ✅ Funciona |
| 19 | Lluvia de Flechas | 50 | 35 | 15s | Damage | ⚠️ Solo single target (debería ser AoE) |

### Sacerdote (Rango 15m)

| ID | Nombre | Valor | Maná | CD | Tipo | Estado |
|----|--------|-------|------|-----|------|--------|
| 6 | Golpe Sagrado | 12 | 8 | 2s | Damage | ✅ Funciona |
| 7 | Palabra Sagrada | 20 | 15 | 4s | Damage | ✅ Funciona |
| 20 | Curación | 35 | 20 | 3s | Heal | ✅ Funciona |
| 21 | Bendición | 60 | 40 | 10s | Heal | ✅ Funciona |
| 22 | Luz Divina | 22 | 15 | 4s | Damage | ✅ Funciona |
| 23 | Castigo Divino | 45 | 30 | 12s | Damage | ✅ Funciona |

---

## Tipos de Habilidad (AbilityType)

```csharp
public enum AbilityType
{
    Damage = 0,  // ✅ Implementado - Aplica daño directo
    Heal = 1,    // ✅ Implementado - Restaura HP
    Buff = 2     // ❌ NO implementado - Solo log de debug
}
```

---

## Lo que FALTA Implementar

### 1. Sistema de Buffs/Debuffs
**Afecta:** Escudo Protector, Grito de Guerra, Barrera de Hielo

```
Necesita:
- Lista de buffs activos por jugador
- Duración temporal
- Modificadores de stats (daño, defensa, velocidad)
- UI para mostrar buffs activos
```

### 2. Área de Efecto (AoE)
**Afecta:** Tajo Giratorio, Tormenta de Fuego, Meteoro, Lluvia de Flechas

```
Necesita:
- Campo "aoeRadius" en AbilityData
- Detección de enemigos en área (Physics.OverlapSphere)
- Aplicar daño a múltiples objetivos
```

### 3. Daño en el Tiempo (DoT)
**Afecta:** Flecha Venenosa, (potencialmente Tormenta de Fuego)

```
Necesita:
- Sistema de efectos activos
- Tick de daño cada X segundos
- Duración del efecto
- UI indicando DoT activo
```

### 4. Habilidades de Posicionamiento
**Afecta:** Carga, Trampa

```
Carga necesita:
- Mover al jugador hacia el objetivo
- Aplicar daño al llegar

Trampa necesita:
- Instanciar objeto en el mundo
- Trigger de activación cuando enemigo pise
```

### 5. Proyectiles Visuales
**Estado actual:** Las habilidades de rango aplican daño instantáneo

```
Necesita:
- Prefabs de proyectiles (flecha, bola de fuego, etc.)
- Sistema de spawn + movimiento hacia target
- Aplicar daño al impactar
```

---

## Validaciones de Combate

El sistema valida antes de ejecutar una habilidad:

| Validación | Dónde | Descripción |
|------------|-------|-------------|
| Cooldown | Cliente + Server | La habilidad no está en cooldown |
| Maná | Cliente + Server | Suficiente maná para el costo |
| Rango | Server | Objetivo dentro del rango de la habilidad |
| Línea de Visión | Server | No hay obstáculos entre jugador y objetivo |
| Zona Segura | Cliente + Server | Ni atacante ni objetivo en zona segura |
| Objetivo Válido | Cliente + Server | El objetivo existe y está vivo |

---

## Eventos del Sistema

```csharp
// PlayerCombat.cs
event Action<int, float> OnCooldownStarted;  // Habilidad entró en cooldown
event Action<int> OnCooldownReady;           // Habilidad lista para usar
event Action OnAbilitiesUpdated;             // Lista de habilidades cambió
event Action<int> OnAbilitySelected;         // Habilidad seleccionada (sistema cruz)

// TargetingSystem.cs
event Action<NetworkIdentity> OnTargetChanged;  // Objetivo cambió
```

---

## Constantes Importantes

```csharp
// PlayerCombat.cs
const float RANGED_ABILITY_THRESHOLD = 3f;  // >= 3m usa sistema de cruz

// TargetingSystem.cs
float maxTargetingDistance = 100f;  // Distancia máxima de targeting
float indicatorYOffset = 0.1f;      // Altura del cursor sobre el suelo
```

---

## UI Relacionada

| Componente | Archivo | Función |
|------------|---------|---------|
| Barra de Habilidades | `AbilityBarUI.cs` | Muestra 6 slots con iconos y cooldowns |
| Indicador de Target | `TargetingSystem.cs` | Círculo rojo bajo el objetivo |
| Cursor Cruz | `TargetingSystem.cs` | Cruz que sigue el mouse (modo Argentum) |
| Frame de Objetivo | `TargetFrameUI.cs` | HP y nombre del objetivo seleccionado |

---

## Notas Técnicas

### Networking (Mirror)
- Las habilidades se ejecutan en el **servidor** (autoridad)
- El cliente envía `CmdUseAbility(index, target)`
- El servidor valida y ejecuta
- Los efectos visuales se sincronizan via `RpcPlayAbilityEffect`
- Los cooldowns se sincronizan via `RpcStartCooldown`

### Carga de Habilidades
- `AbilityDatabase` carga automáticamente desde `Resources/Abilities/`
- Las clases referencian habilidades por GUID en el asset
- Los IDs de habilidad se sincronizan via `SyncList<int>`
