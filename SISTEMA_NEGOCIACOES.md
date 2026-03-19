# 📋 Sistema de Negociações com Confirmação Mútua e Ratings

## ✅ O que foi implementado:

### 1. **Fluxo de Negociação com Confirmação Mútua**

#### Estados da Negociação (Deal):
```
Pending → SellerAccepted → BothAccepted → Completed
```

**Fluxo:**
1. **Pending**: Estado inicial quando comprador cria proposta
2. **SellerAccepted**: Vendedor aceita a proposta
3. **BothAccepted**: Comprador confirma aceitação (ambos aceitaram)
4. **Completed**: Trade foi concluído no jogo
5. **Rejected**: Qualquer um dos lados rejeitou a negociação

#### Endpoints Adicionados:
```
POST /api/deals/{id}/accept-seller   → Vendedor aceita a proposta
POST /api/deals/{id}/accept-buyer    → Comprador confirma (ambos precisam aceitar)
POST /api/deals/{id}/reject          → Rejeitar a negociação
POST /api/deals/{id}/complete        → Confirmar que o trade foi realizado
```

---

### 2. **Rejeição Automática de Outros Deals**

Quando o **comprador confirma a aceitação** (`accept-buyer`):
- Status muda para `BothAccepted`
- **TODOS os outros deals do mesmo pedido são automaticamente rejeitados**
- Apenas um deal pode ser completado por pedido

```csharp
// Exemplo do fluxo:
1. Pedido criado por usuário A
2. Usuário B faz proposta (Deal 1) - Pending
3. Usuário C faz proposta (Deal 2) - Pending
4. Usuário A (vendedor) aceita Deal 1 - SellerAccepted
5. Usuário B (comprador) confirma Deal 1 - BothAccepted
6. Deal 2 é automaticamente rejeitado ❌
```

---

### 3. **Sistema de Ratings Completo**

#### Modelo de Rating:
```csharp
public class Rating
{
    public Guid Id { get; set; }
    public Guid DealId { get; set; }
    public string RaterId { get; set; }        // Quem avaliou
    public string RatedId { get; set; }        // Quem foi avaliado
    public int Stars { get; set; }             // 1-5 ⭐
    public string Comment { get; set; }        // Comentário opcional
    public DateTime CreatedAt { get; set; }
}
```

#### Fluxo de Ratings:
1. Deal é concluído (`Completed`)
2. Ambos podem avaliar o outro (1-5 estrelas + comentário)
3. Reputação do usuário aumenta baseado na nota recebida
4. Histórico de avaliações é armazenado

#### Endpoints:
```
POST /api/deals/{dealId}/rate
{
    "stars": 5,
    "comment": "Excelente negociador, muito honrado!"
}

GET /api/users/{userId}            → Ver perfil com ratings
```

---

### 4. **Perfil de Usuário com Histórico de Ratings**

#### Resposta do Perfil:
```json
{
  "success": true,
  "data": {
    "user": {
      "id": "user-123",
      "email": "player@albion.com",
      "albionNick": "KevinPlayer",
      "serverRegion": "Americas",
      "reputation": 45
    },
    "ratings": [
      {
        "id": "rating-1",
        "dealId": "deal-1",
        "raterId": "user-456",
        "ratedId": "user-123",
        "stars": 5,
        "comment": "Vendedor confiável!",
        "createdAt": "2024-01-15T10:30:00Z"
      },
      {
        "id": "rating-2",
        "dealId": "deal-2",
        "raterId": "user-789",
        "ratedId": "user-123",
        "stars": 4,
        "comment": "Bom negociador",
        "createdAt": "2024-01-14T15:20:00Z"
      }
    ],
    "averageRating": 4.5
  }
}
```

**Informações Exibidas:**
- ⭐ **Média de Avaliações**: Ex: 4.5/5.0
- 📊 **Total de Ratings**: Quantas vezes foi avaliado
- 💬 **Comentários**: Feedback de outros usuários
- 🎖️ **Reputação**: Pontos acumulados
- 📅 **Histórico**: Todas as avaliações recebidas

---

### 5. **Mudanças no Schema do Banco de Dados**

#### Nova Tabela: Ratings
```sql
CREATE TABLE Ratings (
    Id UNIQUEIDENTIFIER PRIMARY KEY,
    DealId UNIQUEIDENTIFIER NOT NULL,
    RaterId NVARCHAR(450) NOT NULL,
    RatedId NVARCHAR(450) NOT NULL,
    Stars INT NOT NULL,
    Comment NVARCHAR(500) NOT NULL,
    CreatedAt DATETIME2 NOT NULL,
    INDEX IX_RatedId (RatedId),
    INDEX IX_RaterId (RaterId),
    INDEX IX_DealId (DealId)
);
```

#### Alterações na Tabela Deals
```sql
ALTER TABLE Deals ADD COLUMN BuyerConfirmed BIT DEFAULT 0;
ALTER TABLE Deals ADD COLUMN SellerConfirmed BIT DEFAULT 0;
```

#### Novos Estados de Deal
```
Pending → SellerAccepted → BothAccepted → Completed
         → Rejected (em qualquer momento)
```

---

## 🎯 Fluxo de Negociação Completo

```
┌─────────────────────────────────────────────────────────────────┐
│ 1. PEDIDO CRIADO                                                │
│    Vendedor A cria pedido para vender 1000 Gold por 50k Silver  │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ 2. MÚLTIPLAS PROPOSTAS                                          │
│    Comprador B: propõe 45k Silver → Deal 1 (Pending)           │
│    Comprador C: propõe 48k Silver → Deal 2 (Pending)           │
│    Comprador D: propõe 50k Silver → Deal 3 (Pending)           │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ 3. VENDEDOR ESCOLHE                                             │
│    Vendedor A aceita Deal 1 (45k) → SellerAccepted             │
│    Deal 2 e Deal 3 continuam aguardando (Pending)              │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ 4. COMPRADOR CONFIRMA                                           │
│    Comprador B confirma → BothAccepted                          │
│    Deal 2 → Rejected ❌                                          │
│    Deal 3 → Rejected ❌                                          │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ 5. NEGOCIAÇÃO NO JOGO                                           │
│    Ambos trocam itens no Albion Online                          │
│    Deal 1 status: BothAccepted                                  │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ 6. CONFIRMAR CONCLUSÃO                                          │
│    Qualquer um clica "Confirmar Trade Realizado"               │
│    Deal 1 → Completed ✅                                        │
│    Agora ambos podem avaliar                                    │
└─────────────────────────────────────────────────────────────────┘
                              ↓
┌─────────────────────────────────────────────────────────────────┐
│ 7. RATINGS                                                       │
│    Comprador B avalia Vendedor A: ⭐⭐⭐⭐⭐ (5 estrelas)         │
│    "Vendedor muito honesto e confiável!"                        │
│                                                                  │
│    Vendedor A avalia Comprador B: ⭐⭐⭐⭐ (4 estrelas)          │
│    "Bom comprador, negociação tranquila"                        │
└─────────────────────────────────────────────────────────────────┘
```

---

## 📱 Interface do Frontend (Próximas Etapas)

### Página de Deal - Nova UI:

```
┌─────────────────────────────────────────────────────┐
│ Status: Pending                                     │
├─────────────────────────────────────────────────────┤
│                                                     │
│ Proposta: 45.000 Silver                            │
│ Comprador: Você                                    │
│ Vendedor: KevinPlayer                              │
│                                                     │
│ ┌─────────────────────────────────────────────┐   │
│ │ Ações:                                      │   │
│ │ [ ] Aceitar proposta (Vendedor)            │   │
│ │ [ ] Confirmar aceitação (Comprador)        │   │
│ │ [ ] Rejeitar                               │   │
│ │ [ ] Confirmar trade realizado ✓           │   │
│ └─────────────────────────────────────────────┘   │
│                                                     │
│ [ ] Avaliar ⭐ (após concluir)                    │
└─────────────────────────────────────────────────────┘
```

### Perfil do Usuário - Nova Aba de Ratings:

```
┌─────────────────────────────────────────────────────┐
│ KevinPlayer - Americas                              │
├─────────────────────────────────────────────────────┤
│                                                     │
│ 🎖️ Reputação: 120 pontos                           │
│ ⭐ Avaliação Média: 4.7/5.0 (23 avaliações)       │
│                                                     │
│ ┌─────────────────────────────────────────────┐   │
│ │ Avaliações Recebidas:                       │   │
│ ├─────────────────────────────────────────────┤   │
│ │ ⭐⭐⭐⭐⭐ Player123                           │   │
│ │ "Vendedor de confiança, entrega sempre!"   │   │
│ │ 15 de janeiro                               │   │
│ ├─────────────────────────────────────────────┤   │
│ │ ⭐⭐⭐⭐ Player456                            │   │
│ │ "Bom negociador, recomendo"                │   │
│ │ 12 de janeiro                               │   │
│ ├─────────────────────────────────────────────┤   │
│ │ ⭐⭐⭐⭐⭐ Player789                           │   │
│ │ "Excelente! Voltaria a negociar"           │   │
│ │ 10 de janeiro                               │   │
│ └─────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────┘
```

---

## 🔧 Como Usar (API)

### 1. Criar Deal
```bash
POST /api/deals
{
  "orderId": "550e8400-e29b-41d4-a716-446655440000",
  "proposedPrice": 45000
}
```

### 2. Vendedor Aceita
```bash
POST /api/deals/{dealId}/accept-seller
```

### 3. Comprador Confirma (rejeita outros)
```bash
POST /api/deals/{dealId}/accept-buyer
```

### 4. Confirmar Trade Realizado
```bash
POST /api/deals/{dealId}/complete
```

### 5. Avaliar o Outro Usuário
```bash
POST /api/deals/{dealId}/rate
{
  "stars": 5,
  "comment": "Excelente negociador!"
}
```

### 6. Ver Perfil com Ratings
```bash
GET /api/users/{userId}
```

---

## ⚠️ Validações Implementadas

✅ Apenas vendedor pode aceitar proposta  
✅ Apenas comprador pode confirmar aceitação  
✅ Apenas participantes podem avaliar  
✅ Não pode avaliar a si mesmo  
✅ Cada usuário avalia outro apenas uma vez por deal  
✅ Apenas deals completados podem ser avaliados  
✅ Ratings entre 1-5 estrelas (obrigatório)  
✅ Comentários não podem ser vazios  
✅ Automático: rejeitar outras negociações quando uma é aceita  

---

## 📊 Estatísticas por Usuário

Agora disponível no perfil:
- **Média de avaliações**: Nota média (ex: 4.5/5)
- **Total de ratings**: Quantas vezes foi avaliado
- **Reputação acumulada**: Soma de todas as notas
- **Histórico completo**: Todos os comentários recebidos

---

## ✨ Próximas Melhorias (Futuro)

- [ ] Filtrar deals por status na UI
- [ ] Mostrar modal de avaliação ao completar deal
- [ ] Medalhas/badges para usuários com altas avaliações
- [ ] Sistema de trust score (comprador/vendedor confiável)
- [ ] Reportar usuários ruins
- [ ] Histórico de deals do usuário
- [ ] Estatísticas de sucesso de negociações
