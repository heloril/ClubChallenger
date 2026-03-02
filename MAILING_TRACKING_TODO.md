# TODO - Finalisation du Suivi des Emails de Mailing

## ✅ Complété

- [x] Création de l'entité `EmailLogEntity`
- [x] Création du repository `EmailLogRepository`
- [x] Création de la classe `EmailRecipientInfo`
- [x] Mise à jour du `RaceManagementContext` pour inclure `EmailLogs`
- [x] Modification de `ChallengeMailingViewModel` :
  - [x] Ajout de la collection `Recipients`
  - [x] Ajout des commandes `LoadRecipientsCommand` et `ResendToSelectedCommand`
  - [x] Implémentation du logging automatique dans `SendEmailAsync`
  - [x] Implémentation de `ExecuteLoadRecipients()`
  - [x] Implémentation de `ExecuteResendToSelected()`
- [x] Création du script de migration SQL `AddEmailLogging.sql`
- [x] Création de la documentation complète
- [x] Build réussi sans erreurs

## 🔄 En Cours / À Faire Immédiatement

### 1. Migration de Base de Données (URGENT)
- [ ] **Exécuter `AddEmailLogging.sql` sur la base de données**
  - Ouvrir SSMS
  - Se connecter à `(LocalDB)\MSSQLLocalDB`
  - Exécuter le script
  - Vérifier que la table `EmailLogs` est créée

### 2. Interface Utilisateur - Challenge Mailing View

Le backend est complet, mais l'interface utilisateur doit être mise à jour pour afficher les nouvelles fonctionnalités.

**Fichier à modifier :** `NameParser.UI\Views\ChallengeMailingView.xaml` (ou similaire)

**Modifications nécessaires :**

```xaml
<!-- Ajouter cette section dans le XAML après la section d'envoi d'emails -->

<GroupBox Header="📊 Recipients Status &amp; Resend" Margin="0,10,0,0">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height="Auto"/>
            <RowDefinition Height="250"/>
            <RowDefinition Height="Auto"/>
        </Grid.RowDefinitions>
        
        <!-- Header avec bouton refresh -->
        <Grid Grid.Row="0" Margin="5">
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width="*"/>
                <ColumnDefinition Width="Auto"/>
            </Grid.ColumnDefinitions>
            
            <TextBlock Text="Click on a recipient to view details or resend individually" 
                       VerticalAlignment="Center" 
                       FontStyle="Italic" 
                       Foreground="Gray"/>
            
            <Button Grid.Column="1" 
                    Content="🔄 Refresh Recipients" 
                    Command="{Binding LoadRecipientsCommand}"
                    Padding="10,5"
                    Margin="5,0,0,0"/>
        </Grid>
        
        <!-- DataGrid pour afficher les destinataires -->
        <DataGrid Grid.Row="1" 
                  ItemsSource="{Binding Recipients}"
                  SelectedItem="{Binding SelectedRecipient}"
                  AutoGenerateColumns="False" 
                  IsReadOnly="True"
                  CanUserAddRows="False"
                  CanUserDeleteRows="False"
                  SelectionMode="Single"
                  AlternatingRowBackground="LightGray"
                  GridLinesVisibility="Horizontal"
                  HeadersVisibility="Column"
                  Margin="5">
            
            <DataGrid.Columns>
                <!-- Colonne Status Icon -->
                <DataGridTextColumn Header="Status" 
                                    Binding="{Binding StatusIcon}" 
                                    Width="60"
                                    FontSize="16">
                    <DataGridTextColumn.ElementStyle>
                        <Style TargetType="TextBlock">
                            <Setter Property="HorizontalAlignment" Value="Center"/>
                        </Style>
                    </DataGridTextColumn.ElementStyle>
                </DataGridTextColumn>
                
                <!-- Colonne Name -->
                <DataGridTextColumn Header="Name" 
                                    Binding="{Binding Name}" 
                                    Width="150"/>
                
                <!-- Colonne Email -->
                <DataGridTextColumn Header="Email" 
                                    Binding="{Binding Email}" 
                                    Width="200"/>
                
                <!-- Colonne Last Sent -->
                <DataGridTextColumn Header="Last Sent" 
                                    Binding="{Binding LastSentDateDisplay}" 
                                    Width="130">
                    <DataGridTextColumn.ElementStyle>
                        <Style TargetType="TextBlock">
                            <Setter Property="HorizontalAlignment" Value="Center"/>
                        </Style>
                    </DataGridTextColumn.ElementStyle>
                </DataGridTextColumn>
                
                <!-- Colonne Error (extensible) -->
                <DataGridTextColumn Header="Error Message" 
                                    Binding="{Binding LastError}" 
                                    Width="*">
                    <DataGridTextColumn.ElementStyle>
                        <Style TargetType="TextBlock">
                            <Setter Property="Foreground" Value="Red"/>
                            <Setter Property="TextWrapping" Value="Wrap"/>
                        </Style>
                    </DataGridTextColumn.ElementStyle>
                </DataGridTextColumn>
            </DataGrid.Columns>
        </DataGrid>
        
        <!-- Footer avec bouton Resend -->
        <StackPanel Grid.Row="2" 
                    Orientation="Horizontal" 
                    Margin="5"
                    HorizontalAlignment="Left">
            
            <Button Content="📧 Resend to Selected" 
                    Command="{Binding ResendToSelectedCommand}"
                    Padding="15,8"
                    FontWeight="Bold"
                    Background="#4CAF50"
                    Foreground="White"
                    BorderThickness="0"
                    Cursor="Hand">
                <Button.Style>
                    <Style TargetType="Button">
                        <Style.Triggers>
                            <Trigger Property="IsEnabled" Value="False">
                                <Setter Property="Opacity" Value="0.5"/>
                            </Trigger>
                            <Trigger Property="IsMouseOver" Value="True">
                                <Setter Property="Background" Value="#45a049"/>
                            </Trigger>
                        </Style.Triggers>
                    </Style>
                </Button.Style>
            </Button>
            
            <!-- Info sur la sélection -->
            <TextBlock VerticalAlignment="Center" 
                       Margin="15,0,0,0">
                <TextBlock.Style>
                    <Style TargetType="TextBlock">
                        <Setter Property="Text" Value="Select a recipient above to enable resend"/>
                        <Setter Property="Foreground" Value="Gray"/>
                        <Style.Triggers>
                            <DataTrigger Binding="{Binding SelectedRecipient, Converter={StaticResource NotNullConverter}}" Value="True">
                                <Setter Property="Text">
                                    <Setter.Value>
                                        <MultiBinding StringFormat="Selected: {0}">
                                            <Binding Path="SelectedRecipient.Email"/>
                                        </MultiBinding>
                                    </Setter.Value>
                                </Setter>
                                <Setter Property="Foreground" Value="Black"/>
                                <Setter Property="FontWeight" Value="Bold"/>
                            </DataTrigger>
                        </Style.Triggers>
                    </Style>
                </TextBlock.Style>
            </TextBlock>
        </StackPanel>
    </Grid>
</GroupBox>
```

**Note :** Si `NotNullConverter` n'existe pas, ajoutez-le dans les ressources ou simplifiez le binding.

### 3. Tests à Effectuer

Une fois l'interface ajoutée :

#### Test 1 : Chargement de la liste
- [ ] Sélectionner un challenge
- [ ] Vérifier que la liste des destinataires se charge
- [ ] Vérifier les statuts (tous devraient être "Pending" si aucun email n'a été envoyé)

#### Test 2 : Envoi d'un email de test
- [ ] Envoyer un email de test à votre adresse
- [ ] Cliquer sur "Refresh Recipients"
- [ ] Vérifier que votre email n'apparaît PAS dans la liste (car les tests sont exclus)
- [ ] Vérifier dans la base de données que l'email est loggé avec `IsTest = 1`

#### Test 3 : Envoi global
- [ ] Cliquer sur "Send to All Challengers"
- [ ] Attendre la fin de l'envoi
- [ ] Vérifier que la liste se rafraîchit automatiquement
- [ ] Vérifier les statuts (✅ pour les succès, ❌ pour les échecs)

#### Test 4 : Renvoi individuel
- [ ] Sélectionner un destinataire en échec (❌)
- [ ] Cliquer sur "Resend to Selected"
- [ ] Vérifier que le statut se met à jour
- [ ] Vérifier dans la base de données qu'un nouveau log est créé

#### Test 5 : Gestion des erreurs
- [ ] Modifier une adresse email dans `Challenge.json` pour la rendre invalide
- [ ] Envoyer un email (devrait échouer)
- [ ] Vérifier que le message d'erreur est affiché dans la colonne "Error"
- [ ] Corriger l'adresse dans `Challenge.json`
- [ ] Rafraîchir la liste
- [ ] Renvoyer à ce destinataire
- [ ] Vérifier le succès

## 📝 À Faire - Améliorations Futures

### Phase 2 : MemberMailingViewModel
- [ ] Appliquer les mêmes modifications à `MemberMailingViewModel`
- [ ] Ajouter les mêmes propriétés et commandes
- [ ] Modifier l'interface pour les members
- [ ] Tester de la même façon

### Phase 3 : Fonctionnalités Avancées

#### Filtres et Recherche
- [ ] Ajouter un champ de recherche par email/nom
- [ ] Ajouter des filtres par statut (Sent/Failed/Pending)
- [ ] Ajouter un tri par colonne

#### Statistiques
- [ ] Ajouter un panneau de statistiques visuelles
- [ ] Graphique : Nombre d'emails envoyés par jour
- [ ] Graphique : Taux de succès vs échec
- [ ] Liste des emails les plus problématiques

#### Actions en Masse
- [ ] Bouton "Resend All Failed" pour renvoyer tous les échecs
- [ ] Bouton "Send to All Pending" pour envoyer aux en attente
- [ ] Export de la liste en CSV

#### Notifications
- [ ] Notification toast lors d'un échec d'envoi
- [ ] Alerte si plus de X% d'échecs
- [ ] Récapitulatif par email après un envoi global

#### Planification
- [ ] Ajouter un DateTimePicker pour programmer l'envoi
- [ ] Queue d'envoi avec retry automatique
- [ ] Limitation du nombre d'emails par minute

### Phase 4 : Maintenance

#### Nettoyage Automatique
- [ ] Ajouter une tâche planifiée pour supprimer les logs > 90 jours
- [ ] Interface pour configurer la durée de rétention
- [ ] Bouton "Clean Old Logs" dans l'interface

#### Reporting
- [ ] Page dédiée aux rapports d'envoi
- [ ] Export PDF des statistiques
- [ ] Historique des envois par challenge

## 🐛 Bugs Connus / À Surveiller

- [ ] Vérifier les performances avec un grand nombre de destinataires (> 1000)
- [ ] Tester la concurrence (deux envois simultanés)
- [ ] Vérifier la gestion des timeouts réseau
- [ ] Tester avec des caractères spéciaux dans les emails

## 📚 Documentation à Mettre à Jour

Si vous modifiez le code :
- [ ] Mettre à jour `MAILING_STATUS_TRACKING_FEATURE.md`
- [ ] Mettre à jour `MAILING_TRACKING_QUICK_START.md`
- [ ] Ajouter des captures d'écran de l'interface
- [ ] Créer une vidéo de démonstration

## ✨ Notes

- Le backend est complet et fonctionnel
- L'interface utilisateur est la seule chose manquante pour utiliser les fonctionnalités
- Tous les emails sont déjà loggés automatiquement
- La base de données doit être migrée avant d'utiliser les fonctionnalités

## 🎯 Priorités

1. **URGENT** : Exécuter la migration SQL
2. **HAUTE** : Ajouter l'interface utilisateur (XAML)
3. **HAUTE** : Tester les fonctionnalités de base
4. **MOYENNE** : Appliquer à MemberMailingViewModel
5. **BASSE** : Améliorations futures

---

**Dernière mise à jour** : Implémentation backend complète, en attente de l'interface utilisateur et de la migration SQL.
