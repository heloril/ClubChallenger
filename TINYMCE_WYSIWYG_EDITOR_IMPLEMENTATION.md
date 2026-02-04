# ✅ Éditeur WYSIWYG avec TinyMCE et WebView2

## 📋 Vue d'ensemble

L'onglet **Challenge Mailing** dispose maintenant d'un **éditeur WYSIWYG professionnel** (What You See Is What You Get) basé sur **TinyMCE**, l'un des éditeurs HTML les plus populaires au monde.

### 🎯 Ce qui a été implémenté

- ✅ **Éditeur riche type Word** - Éditez visuellement sans toucher au HTML
- ✅ **TinyMCE intégré** - Éditeur JavaScript de qualité professionnelle
- ✅ **WebView2** - Moteur Microsoft Edge pour le rendu
- ✅ **Synchronisation automatique** - Le HTML est généré automatiquement
- ✅ **Barre d'outils complète** - Tous les outils de formatage nécessaires

---

## 🎨 Interface utilisateur

```
┌────────────────────────────────────────────────────────────┐
│  Challenge Mailing                                         │
├────────────────────────────────────────────────────────────┤
│                                                             │
│  [Generate Email Template] ← Génère le HTML                │
│                                                             │
│  Subject: [________________________________________]        │
│                                                             │
│  ┌──────────────────────────────────────────────────────┐ │
│  │ ✏️ Email Editor (WYSIWYG)                            │ │
│  │ [🔄 Load Template] [💾 Get HTML]                     │ │
│  │ ℹ️ Edit your email like in Word                      │ │
│  ├──────────────────────────────────────────────────────┤ │
│  │ [B] [I] [U] [Colors] [Lists] [Tables] [Links]       │ │
│  ├──────────────────────────────────────────────────────┤ │
│  │                                                       │ │
│  │  🏃 Challenge 2025                                    │ │
│  │  Mise à jour du Challenge - 09 février 2025          │ │
│  │  ────────────────────────────────────────            │ │
│  │                                                       │ │
│  │  📅 Prochaine Course                                  │ │
│  │  Run in Liège - 15/02/2025                           │ │
│  │                                                       │ │
│  │  🏆 Derniers Résultats                                │ │
│  │  ┌────┬──────────────┬────────┬────────┐            │ │
│  │  │Pos │ Nom          │ Temps  │ Points │            │ │
│  │  ├────┼──────────────┼────────┼────────┤            │ │
│  │  │ 1  │ Jean DUPONT  │ 45:23  │  100   │  ← Éditable│ │
│  │  └────┴──────────────┴────────┴────────┘            │ │
│  │                                                       │ │
│  └──────────────────────────────────────────────────────┘ │
│                                                             │
│  Test Email: [test@example.com] [Send Test]               │
│  [Send to All Challengers]                                 │
└────────────────────────────────────────────────────────────┘
```

---

## 🚀 Guide d'utilisation

### 1️⃣ Générer le template initial

1. **Sélectionnez un challenge** dans la liste déroulante
2. **Cliquez sur "Generate Email Template"**
3. Le HTML est généré automatiquement
4. **Cliquez sur "🔄 Load Template"**
5. L'email s'affiche dans l'éditeur WYSIWYG

### 2️⃣ Éditer l'email visuellement

Vous pouvez maintenant éditer comme dans **Microsoft Word** :

#### 📝 Texte et mise en forme

- **Gras** : Sélectionnez le texte et cliquez sur [B]
- **Italique** : Cliquez sur [I]
- **Souligné** : Cliquez sur [U]
- **Barré** : Utilisez le bouton de barre
- **Couleurs** : Changez la couleur du texte ou du fond

#### 📋 Listes

- **Liste à puces** : Cliquez sur l'icône liste
- **Liste numérotée** : Cliquez sur l'icône numéros
- **Indentation** : Augmenter/diminuer avec les boutons

#### 🔗 Liens et images

- **Insérer un lien** : Sélectionnez le texte → icône lien → entrez l'URL
- **Insérer une image** : icône image → entrez l'URL de l'image

#### 📊 Tableaux

- **Créer un tableau** : Menu "Table" → "Insert table"
- **Ajouter des lignes** : Clic droit → "Row" → "Insert row after/before"
- **Ajouter des colonnes** : Clic droit → "Column" → "Insert column..."
- **Fusionner des cellules** : Sélectionner → Clic droit → "Cell" → "Merge cells"
- **Supprimer** : Sélectionner → Clic droit → "Delete row/column/table"

#### 🎨 Alignement

- **Gauche/Centre/Droite/Justifié** : Boutons d'alignement

### 3️⃣ Sauvegarder les modifications

**Option A : Automatique**
- Les modifications sont sauvegardées automatiquement quand vous cliquez hors de l'éditeur
- Ou quand vous changez de champ

**Option B : Manuel**
- Cliquez sur **"💾 Get HTML"**
- Le HTML est récupéré et sauvegardé dans le ViewModel

### 4️⃣ Envoyer l'email

1. **Test** : Envoyez d'abord un email de test
2. **Vérifiez** : Ouvrez l'email reçu
3. **Envoi massif** : Cliquez sur "Send to All Challengers"

---

## 🛠️ Barre d'outils TinyMCE

### Outils disponibles

| Icône/Bouton | Fonction | Raccourci |
|--------------|----------|-----------|
| **Undo/Redo** | Annuler/Refaire | Ctrl+Z / Ctrl+Y |
| **Blocks** | Paragraphe, Titres (H1-H6) | - |
| **B** | Gras | Ctrl+B |
| **I** | Italique | Ctrl+I |
| **U** | Souligné | Ctrl+U |
| **S** | Barré | - |
| **Align** | Alignement gauche/centre/droite/justifié | - |
| **Bullets** | Liste à puces | - |
| **Numbers** | Liste numérotée | - |
| **Indent** | Augmenter/diminuer l'indentation | - |
| **Colors** | Couleur du texte et du fond | - |
| **Table** | Insérer et gérer des tableaux | - |
| **Link** | Insérer un lien hypertexte | Ctrl+K |
| **Image** | Insérer une image | - |
| **Remove Format** | Supprimer toute la mise en forme | - |
| **Code** | Voir/éditer le code HTML source | - |
| **Help** | Aide TinyMCE | - |

---

## 💡 Exemples de modifications

### Exemple 1 : Modifier un titre

**Avant:**
```
🏃 Challenge 2025
```

**Comment faire:**
1. Double-cliquez sur "Challenge 2025"
2. Tapez le nouveau texte : "Super Challenge 2025"
3. Changez la couleur si vous voulez (bouton couleur)

**Résultat:**
```
🏃 Super Challenge 2025
```

### Exemple 2 : Ajouter une ligne dans un tableau

**Comment faire:**
1. Cliquez dans une cellule du tableau
2. Clic droit → "Row" → "Insert row after"
3. Remplissez les cellules

**Avant:**
| Pos | Nom | Temps | Points |
|-----|-----|-------|--------|
| 1 | Jean DUPONT | 45:23 | 100 |
| 2 | Marie MARTIN | 46:12 | 95 |

**Après:**
| Pos | Nom | Temps | Points |
|-----|-----|-------|--------|
| 1 | Jean DUPONT | 45:23 | 100 |
| 2 | Marie MARTIN | 46:12 | 95 |
| 3 | Pierre BERNARD | 47:05 | 90 |

### Exemple 3 : Changer la couleur d'un en-tête de tableau

**Comment faire:**
1. Sélectionnez la ligne d'en-tête
2. Cliquez sur le bouton "Background color"
3. Choisissez une nouvelle couleur (ex: bleu)

### Exemple 4 : Ajouter un message important

**Comment faire:**
1. Placez le curseur où vous voulez insérer le message
2. Tapez votre texte
3. Sélectionnez le texte
4. Changez la couleur de fond (ex: jaune clair)
5. Changez la couleur du texte (ex: rouge)

**Résultat:**
```
⚡ IMPORTANT: Inscriptions ouvertes jusqu'au 28 février !
```

---

## ⚙️ Fonctionnalités avancées

### Mode Code Source

Si vous devez accéder au HTML brut :
1. Cliquez sur le bouton **"Code"** dans la barre d'outils
2. Éditez le HTML directement
3. Cliquez sur **"Save"**
4. L'éditeur WYSIWYG se met à jour

### Copier-coller depuis Word

Vous pouvez copier du contenu depuis Microsoft Word :
1. **Copiez** le contenu dans Word (Ctrl+C)
2. **Collez** dans l'éditeur TinyMCE (Ctrl+V)
3. TinyMCE nettoie automatiquement le HTML

⚠️ **Note** : Certains styles complexes de Word peuvent être simplifiés.

### Rechercher et remplacer

1. Menu **"Edit"** → **"Find and replace"**
2. Entrez le texte à rechercher
3. Entrez le texte de remplacement
4. Cliquez sur "Replace" ou "Replace all"

---

## 🔧 Architecture technique

### Composants utilisés

1. **WebView2** (`Microsoft.Web.WebView2`)
   - Moteur Microsoft Edge Chromium
   - Rendu HTML5/CSS3/JavaScript moderne
   - Communication bidirectionnelle C# ↔ JavaScript

2. **TinyMCE 6** (CDN)
   - Éditeur WYSIWYG JavaScript
   - Open source et gratuit
   - Plugins : tables, listes, liens, images, code, etc.

3. **Communication**
   - C# → JavaScript : `ExecuteScriptAsync()`
   - JavaScript → C# : `WebMessageReceived` event

### Flux de données

```
1. User clicks "Generate Template"
   ↓
2. ViewModel generates HTML
   ↓
3. C# détecte le changement (PropertyChanged)
   ↓
4. C# envoie le HTML à TinyMCE via ExecuteScriptAsync()
   ↓
5. TinyMCE affiche l'email éditable
   ↓
6. User édite dans TinyMCE
   ↓
7. TinyMCE envoie le HTML modifié à C# (WebMessageReceived)
   ↓
8. C# met à jour le ViewModel
   ↓
9. Email prêt à être envoyé
```

---

## 📦 Prérequis

### Microsoft Edge WebView2 Runtime

WebView2 nécessite **Microsoft Edge WebView2 Runtime**:

✅ **Généralement déjà installé** sur Windows 10/11
✅ **Téléchargement automatique** si manquant
✅ **Taille** : ~150 MB

**Si l'éditeur ne se charge pas:**
1. Téléchargez manuellement : https://developer.microsoft.com/microsoft-edge/webview2/
2. Installez "Evergreen Standalone Installer"
3. Redémarrez l'application

---

## 🐛 Dépannage

### Problème : L'éditeur ne s'affiche pas

**Solutions:**
1. Vérifiez que Edge WebView2 Runtime est installé
2. Vérifiez le message d'erreur dans la MessageBox
3. Téléchargez et installez WebView2 Runtime
4. Redémarrez l'application

### Problème : Le template ne se charge pas

**Solutions:**
1. Cliquez sur "Generate Email Template"
2. Attendez que le HTML soit généré
3. Cliquez sur **"🔄 Load Template"**
4. Vérifiez que l'éditeur est bien initialisé

### Problème : Les modifications ne sont pas sauvegardées

**Solutions:**
1. Cliquez hors de l'éditeur (sauvegarde automatique)
2. Ou cliquez sur **"💾 Get HTML"** (sauvegarde manuelle)
3. Vérifiez que le HTML dans le ViewModel est mis à jour

### Problème : TinyMCE affiche "Failed to load"

**Solutions:**
1. Vérifiez votre connexion Internet (TinyMCE est chargé depuis le CDN)
2. Si pas d'Internet, TinyMCE ne se chargera pas
3. Alternative : Télécharger TinyMCE localement (voir section Alternative)

---

## 🌐 Alternative sans Internet (TinyMCE local)

Si vous n'avez pas accès à Internet, vous pouvez télécharger TinyMCE localement :

### Étapes

1. **Télécharger TinyMCE**
   - https://www.tiny.cloud/get-tiny/self-hosted/
   - Téléchargez "Community Edition (Free)"

2. **Extraire dans le projet**
   - Créez le dossier `NameParser.UI\Resources\tinymce\`
   - Extrayez les fichiers dedans

3. **Modifier TinyMCEEditor.html**
   ```html
   <!-- Au lieu de CDN -->
   <script src="https://cdn.tiny.cloud/1/.../tinymce.min.js"></script>
   
   <!-- Utilisez le fichier local -->
   <script src="tinymce/tinymce.min.js"></script>
   ```

4. **Mettre à jour le .csproj**
   ```xml
   <None Update="Resources\tinymce\**\*">
     <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
   </None>
   ```

---

## ✅ Avantages de cette solution

### 🎯 Pour l'utilisateur

✅ **Édition intuitive** - Comme dans Word, aucune connaissance HTML requise
✅ **WYSIWYG** - Ce que vous voyez est ce qui sera envoyé
✅ **Tous les outils** - Formatage complet (gras, couleurs, tableaux, etc.)
✅ **Erreurs réduites** - Pas de risque de casser le HTML
✅ **Copier-coller** - Depuis Word ou d'autres sources
✅ **Professionnel** - Éditeur de qualité industrielle

### 🔧 Pour le développeur

✅ **TinyMCE** - Éditeur open source populaire et bien maintenu
✅ **WebView2** - Moteur moderne (Edge Chromium)
✅ **Synchronisation** - HTML automatiquement généré et sauvegardé
✅ **Extensible** - Facile d'ajouter des plugins TinyMCE
✅ **Pas de conversion** - TinyMCE gère le HTML nativement

---

## 🚀 Améliorations futures possibles

### 1. Templates personnalisés
- [ ] Sauvegarder des templates d'emails
- [ ] Charger des templates prédéfinis
- [ ] Galerie de templates

### 2. Plugins additionnels TinyMCE
- [ ] Vérificateur orthographique
- [ ] Insertion d'émojis avancée
- [ ] Galerie d'images
- [ ] Templates de tableaux

### 3. Prévisualisation multi-clients
- [ ] Aperçu Gmail
- [ ] Aperçu Outlook
- [ ] Aperçu mobile

### 4. Version locale de TinyMCE
- [ ] Télécharger TinyMCE localement
- [ ] Fonctionnement sans Internet

---

## 📚 Ressources

- **TinyMCE Documentation** : https://www.tiny.cloud/docs/
- **WebView2 Documentation** : https://learn.microsoft.com/microsoft-edge/webview2/
- **TinyMCE Playground** : https://www.tiny.cloud/tinymce-playground/

---

**Date de mise en œuvre** : 2025-02-09  
**Version** : 3.0 - Éditeur WYSIWYG avec TinyMCE + WebView2  
**Build** : ✅ Réussi  
**Prérequis** : Edge WebView2 Runtime (généralement déjà installé)
