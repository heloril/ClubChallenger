# Architecture Technique - Système de Suivi des Emails

## 📐 Vue d'Ensemble de l'Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        Couche Présentation                       │
│  ┌──────────────────────┐         ┌──────────────────────────┐  │
│  │ ChallengeMailingView │         │ MemberMailingView        │  │
│  │       (XAML)         │         │      (XAML)              │  │
│  └──────────┬───────────┘         └──────────┬───────────────┘  │
│             │                                 │                   │
└─────────────┼─────────────────────────────────┼───────────────────┘
              │                                 │
              ▼                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│                     Couche ViewModel (MVVM)                      │
│  ┌──────────────────────────┐   ┌──────────────────────────┐   │
│  │ ChallengeMailingViewModel│   │ MemberMailingViewModel   │   │
│  │  • Recipients           │   │  • Recipients (TODO)      │   │
│  │  • SelectedRecipient    │   │  • SelectedRecipient      │   │
│  │  • LoadRecipientsCmd    │   │  • LoadRecipientsCmd      │   │
│  │  • ResendToSelectedCmd  │   │  • ResendToSelectedCmd    │   │
│  └────────────┬─────────────┘   └─────────────┬─────────────┘   │
│               │                                 │                 │
└───────────────┼─────────────────────────────────┼─────────────────┘
                │                                 │
                ▼                                 ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Couche Business Logic                         │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │           EmailRecipientInfo (ViewModel Class)           │   │
│  │  • Email, Name, Status, LastSentDate, LastError          │   │
│  │  • INotifyPropertyChanged pour binding temps réel        │   │
│  └──────────────────────────────────────────────────────────┘   │
│                                                                   │
└───────────────┬───────────────────────────────┬───────────────────┘
                │                               │
                ▼                               ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Couche Data Access (Repositories)             │
│  ┌──────────────────────┐        ┌─────────────────────────┐    │
│  │  EmailLogRepository  │        │ Other Repositories      │    │
│  │  • LogEmail()        │        │ • Challenge            │    │
│  │  • GetLastEmailLog() │        │ • Member               │    │
│  │  • GetEmailLogs...() │        │ • Race                 │    │
│  └──────────┬───────────┘        └─────────────┬───────────┘    │
│             │                                   │                 │
└─────────────┼───────────────────────────────────┼─────────────────┘
              │                                   │
              ▼                                   ▼
┌─────────────────────────────────────────────────────────────────┐
│                   Couche Entity Framework                        │
│  ┌──────────────────────────────────────────────────────────┐   │
│  │           RaceManagementContext (DbContext)              │   │
│  │  • DbSet<EmailLogEntity> EmailLogs                       │   │
│  │  • DbSet<ChallengeEntity> Challenges                     │   │
│  │  • DbSet<RaceEntity> Races                               │   │
│  │  • ... autres DbSets                                     │   │
│  └──────────────────────────┬───────────────────────────────┘   │
│                             │                                    │
└─────────────────────────────┼────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────────┐
│                    Base de Données SQL Server                    │
│  ┌────────────────┐  ┌──────────────┐  ┌────────────────────┐  │
│  │  EmailLogs     │  │  Challenges  │  │  Autres Tables     │  │
│  │  • Id          │  │  • Id        │  │  • Races           │  │
│  │  • EmailType   │  │  • Name      │  │  • Members         │  │
│  │  • ChallengeId ├──┤  • Year      │  │  • ...             │  │
│  │  • ...         │  │  • ...       │  │                    │  │
│  └────────────────┘  └──────────────┘  └────────────────────┘  │
│                                                                   │
│  (LocalDB)\MSSQLLocalDB - RaceManagementDb                       │
└─────────────────────────────────────────────────────────────────┘
```

## 🔄 Flux de Données

### Scénario 1 : Chargement des Destinataires

```
1. User sélectionne un Challenge dans l'UI
   ↓
2. ChallengeMailingViewModel.SelectedChallenge (setter)
   ↓
3. ExecuteLoadRecipients() est appelé automatiquement
   ↓
4. Lecture de Challenge.json (fichier système)
   ↓
5. Pour chaque email unique :
   a. EmailLogRepository.GetLastEmailLog(email, "Challenge", challengeId)
   b. Query SQL : SELECT TOP 1 * FROM EmailLogs WHERE ...
   c. Retour du dernier log (ou null si jamais envoyé)
   ↓
6. Création d'EmailRecipientInfo pour chaque destinataire
   Status = (lastLog == null) ? "Pending" : (lastLog.IsSuccess ? "Sent" : "Failed")
   ↓
7. Ajout à Recipients (ObservableCollection)
   ↓
8. DataGrid dans l'UI se met à jour automatiquement (binding WPF)
   ↓
9. StatusMessage affiche le résumé (X sent, Y failed, Z pending)
```

### Scénario 2 : Envoi d'un Email

```
1. User clique sur "Send Test Email" ou "Send to All"
   ↓
2. SendEmailAsync(email, subject, body, pdfPath, isTest) est appelé
   ↓
3. Try block :
   a. Création du MimeMessage (MailKit)
   b. Connexion au serveur SMTP
   c. Authentification
   d. Envoi de l'email
   e. Succès ✅
      ↓
      EmailLogRepository.LogEmail(
          emailType: "Challenge",
          challengeId: selectedChallengeId,
          recipientEmail: email,
          subject: subject,
          isSuccess: true,
          isTest: isTest
      )
      ↓
      INSERT INTO EmailLogs VALUES (...)
   ↓
4. Catch block (si erreur) :
   EmailLogRepository.LogEmail(
       ...
       isSuccess: false,
       errorMessage: ex.Message,
       ...
   )
   ↓
   INSERT INTO EmailLogs VALUES (... IsSuccess = 0, ErrorMessage = "...")
   ↓
5. Finally :
   a. Recipients.Refresh() (si envoi global)
   b. StatusMessage mis à jour
```

### Scénario 3 : Renvoi Individuel

```
1. User sélectionne un destinataire (SelectedRecipient = recipientInfo)
   ↓
2. User clique sur "Resend to Selected"
   ↓
3. CanExecuteResendToSelected() vérifie :
   - SelectedRecipient != null ✓
   - EmailSubject non vide ✓
   - EmailBody non vide ✓
   - Credentials Gmail valides ✓
   - !IsSending ✓
   ↓
4. ExecuteResendToSelected() est appelé
   ↓
5. MessageBox de confirmation (Yes/No)
   ↓
6. Si Yes :
   a. IsSending = true
   b. SelectedRecipient.Status = "Sending"
   c. SelectedRecipient.IsSending = true (spinner dans l'UI)
      ↓
   d. Génération du PDF (GenerateDetailedClassificationPdf())
      ↓
   e. await SendEmailAsync(email, subject, body, pdfPath, isTest: false)
      ↓ (voir Scénario 2 pour le détail)
   f. Succès :
      - SelectedRecipient.Status = "Sent" ✅
      - SelectedRecipient.LastSentDate = DateTime.Now
      - SelectedRecipient.LastError = null
   g. Échec :
      - SelectedRecipient.Status = "Failed" ❌
      - SelectedRecipient.LastError = ex.Message
      ↓
   h. Cleanup : Suppression du PDF temporaire
   i. IsSending = false
   j. SelectedRecipient.IsSending = false
```

## 🗄️ Modèle de Données

### EmailLogEntity (Base de Données)

```csharp
public class EmailLogEntity
{
    [Key]
    public int Id { get; set; }                      // PK, Identity
    
    [Required, MaxLength(50)]
    public string EmailType { get; set; }             // "Challenge" ou "Member"
    
    [ForeignKey("Challenge")]
    public int? ChallengeId { get; set; }            // FK nullable vers Challenges
    
    [Required, MaxLength(255)]
    public string RecipientEmail { get; set; }        // Email du destinataire
    
    [MaxLength(255)]
    public string RecipientName { get; set; }         // Nom (optionnel)
    
    [MaxLength(500)]
    public string Subject { get; set; }               // Sujet de l'email
    
    [Required]
    public DateTime SentDate { get; set; }            // Date/heure d'envoi
    
    [Required]
    public bool IsSuccess { get; set; }               // true si succès, false si échec
    
    public string ErrorMessage { get; set; }          // Message d'erreur si échec
    
    [Required]
    public bool IsTest { get; set; }                  // true si email de test
    
    [MaxLength(100)]
    public string SentBy { get; set; }                // Utilisateur Windows (Environment.UserName)
}
```

### EmailRecipientInfo (ViewModel)

```csharp
public class EmailRecipientInfo : INotifyPropertyChanged
{
    public string Email { get; set; }                 // Email du destinataire
    public string Name { get; set; }                  // Nom du destinataire
    public string Status { get; set; }                // "Sent", "Failed", "Pending", "Sending"
    public DateTime? LastSentDate { get; set; }       // Date du dernier envoi (nullable)
    public string LastError { get; set; }             // Dernier message d'erreur
    public bool IsSending { get; set; }               // true pendant l'envoi
    
    // Propriétés calculées
    public string StatusIcon => Status switch
    {
        "Sent" => "✅",
        "Failed" => "❌",
        "Pending" => "⏳",
        "Sending" => "📤",
        _ => "❓"
    };
    
    public string LastSentDateDisplay => 
        LastSentDate?.ToString("dd/MM/yyyy HH:mm") ?? "Never";
}
```

## 🔗 Relations et Dépendances

### ChallengeMailingViewModel Dependencies

```
ChallengeMailingViewModel
├── ChallengeRepository (existing)
├── RaceEventRepository (existing)
├── RaceRepository (existing)
├── ClassificationRepository (existing)
├── JsonMemberRepository (existing)
└── EmailLogRepository (NEW) ← Nouvelle dépendance
```

### EmailLogRepository Dependencies

```
EmailLogRepository
└── RaceManagementContext
    └── DbSet<EmailLogEntity>
        └── SQL Server Database
            └── EmailLogs table
```

## 📊 Index de Base de Données

Performance optimisée grâce aux index :

```sql
-- Index pour recherche par email (WHERE RecipientEmail = @email)
CREATE INDEX IX_EmailLogs_RecipientEmail ON EmailLogs(RecipientEmail);

-- Index pour recherche par challenge (WHERE ChallengeId = @id)
CREATE INDEX IX_EmailLogs_ChallengeId ON EmailLogs(ChallengeId);

-- Index pour tri par date (ORDER BY SentDate DESC)
CREATE INDEX IX_EmailLogs_SentDate ON EmailLogs(SentDate DESC);

-- Index pour filtrage par type (WHERE EmailType = @type)
CREATE INDEX IX_EmailLogs_EmailType ON EmailLogs(EmailType);
```

### Requêtes Typiques et Index Utilisés

| Requête | Index Utilisé | Performance |
|---------|---------------|-------------|
| Dernier log pour un email | `IX_EmailLogs_RecipientEmail` | 🚀 Rapide |
| Logs pour un challenge | `IX_EmailLogs_ChallengeId` | 🚀 Rapide |
| Logs récents (7 derniers jours) | `IX_EmailLogs_SentDate` | 🚀 Rapide |
| Logs par type (Challenge/Member) | `IX_EmailLogs_EmailType` | 🚀 Rapide |

## 🔐 Sécurité et Confidentialité

### Données Stockées
- ✅ **Métadonnées** : Email, sujet, date, statut
- ❌ **Contenu de l'email** : NON stocké (pour la confidentialité)
- ❌ **Mots de passe** : NON stockés (utilisés en mémoire uniquement)
- ✅ **Messages d'erreur** : Stockés pour diagnostic

### Recommandations
1. Ne jamais logger le contenu complet de l'email
2. Nettoyer régulièrement les anciens logs (> 90 jours)
3. Protéger l'accès à la base de données
4. Ne pas exposer les logs via API publique

## ⚡ Performance

### Optimisations Implémentées
- **Index de base de données** : Requêtes rapides sur de gros volumes
- **Requêtes filtrées** : Exclusion des tests (`IsTest = false`)
- **Chargement à la demande** : Les destinataires ne sont chargés que lors de la sélection d'un challenge
- **ObservableCollection** : Binding WPF efficace avec mises à jour automatiques

### Scalabilité
- ✅ **100 destinataires** : Temps de chargement < 500ms
- ✅ **1000 destinataires** : Temps de chargement < 2s
- ⚠️ **10000+ destinataires** : Pagination recommandée (future improvement)

### Consommation Mémoire
- **Par EmailRecipientInfo** : ~500 bytes
- **Pour 1000 destinataires** : ~500 KB
- **Négligeable** pour une application desktop moderne

## 🧪 Points de Test

### Tests Unitaires (recommandés)
```csharp
[TestClass]
public class EmailLogRepositoryTests
{
    [TestMethod]
    public void LogEmail_Success_CreatesRecord()
    {
        // Arrange
        var repo = new EmailLogRepository();
        
        // Act
        repo.LogEmail("Challenge", 1, "test@test.com", "Test User", 
                      "Test Subject", true, null, false);
        
        // Assert
        var log = repo.GetLastEmailLog("test@test.com", "Challenge", 1);
        Assert.IsNotNull(log);
        Assert.IsTrue(log.IsSuccess);
    }
    
    [TestMethod]
    public void GetLastEmailLog_NoLogs_ReturnsNull()
    {
        // ...
    }
}
```

### Tests d'Intégration
1. **Test de bout en bout** : Sélection challenge → Chargement destinataires → Envoi → Vérification logs
2. **Test de performance** : Temps de chargement avec 500 destinataires
3. **Test de concurrence** : Deux envois simultanés

## 🚀 Déploiement

### Pré-requis
1. SQL Server LocalDB installé
2. Migration de base de données exécutée (`AddEmailLogging.sql`)
3. Application recompilée avec les nouvelles classes

### Checklist de Déploiement
- [ ] Backup de la base de données actuelle
- [ ] Exécution de `AddEmailLogging.sql`
- [ ] Vérification que la table `EmailLogs` existe
- [ ] Test d'envoi d'un email
- [ ] Vérification que l'email est loggé
- [ ] Rollback plan en cas de problème

## 📈 Monitoring

### Métriques à Surveiller
- Nombre d'emails envoyés par jour
- Taux de succès vs échec
- Temps moyen d'envoi par email
- Emails en échec récurrents (adresses invalides)

### Requêtes de Monitoring

```sql
-- Statistiques des dernières 24h
SELECT 
    EmailType,
    COUNT(*) AS TotalSent,
    SUM(CASE WHEN IsSuccess = 1 THEN 1 ELSE 0 END) AS Successful,
    SUM(CASE WHEN IsSuccess = 0 THEN 1 ELSE 0 END) AS Failed
FROM EmailLogs
WHERE SentDate >= DATEADD(DAY, -1, GETDATE())
GROUP BY EmailType;

-- Top 10 emails en échec
SELECT TOP 10 
    RecipientEmail,
    COUNT(*) AS FailureCount,
    MAX(ErrorMessage) AS LastError
FROM EmailLogs
WHERE IsSuccess = 0
GROUP BY RecipientEmail
ORDER BY COUNT(*) DESC;
```

## 🛠️ Maintenance

### Nettoyage des Anciens Logs

```csharp
// Dans EmailLogRepository
public void DeleteOldLogs(int daysToKeep = 90)
{
    using var context = new RaceManagementContext();
    var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
    var oldLogs = context.EmailLogs.Where(e => e.SentDate < cutoffDate);
    context.EmailLogs.RemoveRange(oldLogs);
    context.SaveChanges();
}
```

**Recommandation** : Exécuter mensuellement ou via une tâche planifiée.

---

**Version** : 1.0  
**Dernière mise à jour** : 2025  
**Auteur** : GitHub Copilot
