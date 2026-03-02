# Fonctionnalités de Suivi des Emails (Mailing)

## Vue d'ensemble

De nouvelles fonctionnalités ont été ajoutées au système de mailing pour permettre de :
1. ✅ **Visualiser le statut d'envoi** des emails (envoyé, en attente, échec)
2. 🔄 **Renvoyer individuellement** les emails en cas d'erreur
3. 📋 **Afficher une liste complète** des destinataires avec leur statut

## Architecture

### Nouvelles Entités et Classes

#### 1. `EmailLogEntity` (NameParser\Infrastructure\Data\Models\EmailLogEntity.cs)
Entité de base de données pour stocker l'historique des emails envoyés :
- `Id` : Identifiant unique
- `EmailType` : Type d'email ("Challenge" ou "Member")
- `ChallengeId` : ID du challenge (nullable)
- `RecipientEmail` : Email du destinataire
- `RecipientName` : Nom du destinataire
- `Subject` : Sujet de l'email
- `SentDate` : Date d'envoi
- `IsSuccess` : Indicateur de succès/échec
- `ErrorMessage` : Message d'erreur en cas d'échec
- `IsTest` : Indicateur d'email de test
- `SentBy` : Utilisateur ayant envoyé l'email

#### 2. `EmailLogRepository` (NameParser\Infrastructure\Data\EmailLogRepository.cs)
Repository pour gérer les logs d'emails :
- `LogEmail()` : Enregistre un email envoyé
- `GetEmailLogsByChallenge()` : Récupère les logs pour un challenge
- `GetEmailLogsByType()` : Récupère les logs par type
- `GetLastEmailLog()` : Récupère le dernier log pour un destinataire
- `GetRecentEmailLogs()` : Récupère les logs récents
- `DeleteOldLogs()` : Nettoie les anciens logs

#### 3. `EmailRecipientInfo` (NameParser.UI\ViewModels\EmailRecipientInfo.cs)
Classe ViewModel pour afficher les destinataires dans l'interface :
- `Email` : Adresse email
- `Name` : Nom du destinataire
- `Status` : Statut ("Sent", "Failed", "Pending", "Sending")
- `LastSentDate` : Date du dernier envoi
- `LastError` : Dernier message d'erreur
- `StatusIcon` : Icône visuelle (✅, ❌, ⏳, 📤)

### Modifications des ViewModels

#### ChallengeMailingViewModel
Ajouts :
- `ObservableCollection<EmailRecipientInfo> Recipients` : Liste des destinataires
- `EmailRecipientInfo SelectedRecipient` : Destinataire sélectionné
- `LoadRecipientsCommand` : Commande pour charger la liste
- `ResendToSelectedCommand` : Commande pour renvoyer à un destinataire

Méthodes ajoutées :
- `ExecuteLoadRecipients()` : Charge les destinataires depuis Challenge.json et leur statut
- `ExecuteResendToSelected()` : Renvoie l'email au destinataire sélectionné
- Logging automatique dans `SendEmailAsync()` pour tous les envois

#### MemberMailingViewModel
(Modifications similaires à prévoir - voir structure ci-dessous)

## Fonctionnement

### 1. Chargement des Destinataires

Lorsqu'un challenge est sélectionné dans `ChallengeMailingViewModel` :
1. La liste des destinataires est automatiquement chargée depuis `Challenge.json`
2. Pour chaque destinataire, le système vérifie le dernier log d'email
3. Le statut est déterminé :
   - **"Pending"** : Aucun email n'a jamais été envoyé
   - **"Sent"** : Le dernier envoi a réussi
   - **"Failed"** : Le dernier envoi a échoué

### 2. Envoi d'Emails

Tous les envois d'emails sont maintenant loggés :
- **Succès** : Log créé avec `IsSuccess = true`
- **Échec** : Log créé avec `IsSuccess = false` et message d'erreur
- **Tests** : Marqués avec `IsTest = true` (exclus de la liste principale)

### 3. Renvoi Individuel

Pour renvoyer un email à un destinataire spécifique :
1. Sélectionner le destinataire dans la liste
2. Cliquer sur "Resend to Selected" / "Renvoyer"
3. Le système :
   - Génère le PDF du classement
   - Envoie l'email
   - Met à jour le statut en temps réel
   - Enregistre le nouveau log

## Interface Utilisateur

### Challenge Mailing View
Nouvelles sections à ajouter dans le XAML :

```xaml
<GroupBox Header="Recipients Status" Margin="0,10,0,0">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="300"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Bouton Refresh -->
        <Button Grid.Row="0" Content="🔄 Refresh Recipients" 
                Command="{Binding LoadRecipientsCommand}" 
                HorizontalAlignment="Left" Margin="5"/>
        
        <!-- Liste des destinataires -->
        <DataGrid Grid.Row="1" ItemsSource="{Binding Recipients}"
                  SelectedItem="{Binding SelectedRecipient}"
                  AutoGenerateColumns="False" IsReadOnly="True"
                  Margin="5">
            <DataGrid.Columns>
                <DataGridTextColumn Header="Status" Binding="{Binding StatusIcon}" Width="50"/>
                <DataGridTextColumn Header="Name" Binding="{Binding Name}" Width="150"/>
                <DataGridTextColumn Header="Email" Binding="{Binding Email}" Width="200"/>
                <DataGridTextColumn Header="Last Sent" Binding="{Binding LastSentDateDisplay}" Width="120"/>
                <DataGridTextColumn Header="Error" Binding="{Binding LastError}" Width="*"/>
            </DataGrid.Columns>
        </DataGrid>
        
        <!-- Bouton Resend -->
        <StackPanel Grid.Row="2" Orientation="Horizontal" Margin="5">
            <Button Content="📧 Resend to Selected" 
                    Command="{Binding ResendToSelectedCommand}"
                    IsEnabled="{Binding SelectedRecipient, Converter={StaticResource NotNullConverter}}"
                    Padding="10,5"/>
            <TextBlock Text="{Binding StatusMessage}" 
                       VerticalAlignment="Center" 
                       Margin="10,0,0,0"/>
        </StackPanel>
    </Grid>
</GroupBox>
```

## Migration de Base de Données

Voir `DATABASE_EMAIL_LOGGING_MIGRATION.md` pour les instructions de migration.

## Avantages

### Pour l'Administrateur
- ✅ Visibilité complète sur les envois
- ✅ Possibilité de corriger les erreurs ponctuelles
- ✅ Historique complet des communications
- ✅ Identification rapide des problèmes

### Pour le Système
- ✅ Traçabilité complète
- ✅ Détection des adresses email invalides
- ✅ Statistiques d'envoi
- ✅ Audit trail pour la conformité

## Prochaines Étapes

### Pour MemberMailingViewModel
Appliquer les mêmes modifications :
1. Ajouter `Recipients` et `SelectedRecipient`
2. Ajouter `LoadRecipientsCommand` et `ResendToSelectedCommand`
3. Modifier `SendEmailAsync()` pour logger
4. Charger les destinataires depuis Members.json

### Améliorations Futures
1. **Filtres** : Filtrer les destinataires par statut
2. **Statistiques** : Afficher des graphiques d'envoi
3. **Notifications** : Alertes pour les échecs d'envoi
4. **Planification** : Planifier les envois pour plus tard
5. **Templates** : Gestion des templates d'emails

## Utilisation

### Exemple de Workflow

1. **Préparation**
   ```
   - Sélectionner un challenge
   - Générer le template d'email
   - Envoyer un email de test
   ```

2. **Consultation du Statut**
   ```
   - Cliquer sur "Refresh Recipients"
   - Voir la liste avec statuts (✅ Sent, ❌ Failed, ⏳ Pending)
   ```

3. **Envoi Global**
   ```
   - Cliquer sur "Send to All Challengers"
   - Confirmer l'envoi
   - Le système envoie et met à jour les statuts automatiquement
   ```

4. **Correction d'Erreurs**
   ```
   - Identifier les destinataires en échec (❌)
   - Corriger le problème (email, connexion, etc.)
   - Sélectionner le destinataire
   - Cliquer sur "Resend to Selected"
   ```

## Notes Techniques

### Performance
- Les logs sont indexés pour des requêtes rapides
- Possibilité de nettoyer les anciens logs (> 90 jours)
- Exclusion des emails de test de la liste principale

### Sécurité
- Pas de stockage du contenu complet de l'email
- Seules les métadonnées sont conservées
- Tracking de l'utilisateur ayant envoyé l'email

### Compatibilité
- Fonctionne avec l'infrastructure existante
- Pas de modification des fichiers JSON existants
- Rétrocompatible avec les anciennes versions
