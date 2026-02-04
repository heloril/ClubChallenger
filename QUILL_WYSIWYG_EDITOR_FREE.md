# ✅ Éditeur WYSIWYG avec Quill (100% Gratuit)

## 📋 Vue d'ensemble

L'onglet **Challenge Mailing** utilise maintenant **Quill**, un éditeur WYSIWYG **complètement gratuit et open source**, sans besoin de clé API ou d'inscription.

### 🎯 Pourquoi Quill ?

- ✅ **100% Gratuit** - Aucune clé API requise, pas de limite
- ✅ **Open Source** - Code source disponible sur GitHub
- ✅ **Moderne** - Utilisé par des milliers d'applications
- ✅ **Léger** - Plus petit et plus rapide que TinyMCE
- ✅ **Sans dépendances** - Fonctionne hors connexion (une fois chargé)

---

## 🚀 Guide d'utilisation

### 1️⃣ Générer le template

1. Sélectionnez un **challenge**
2. Cliquez sur **"Generate Email Template"**
3. Cliquez sur **"🔄 Load Template"**

### 2️⃣ Éditer avec Quill

L'éditeur ressemble à ceci :

```
┌────────────────────────────────────────────────────────┐
│ [B] [I] [U] [Color ▼] [Background ▼] [Header ▼]       │
│ [•] [1.] [Align ▼] [Link] [Clean]                     │
├────────────────────────────────────────────────────────┤
│                                                         │
│  🏃 Challenge 2025                                      │
│                                                         │
│  Mise à jour du Challenge - 09 février 2025           │
│  ─────────────────────────────────────                 │
│                                                         │
│  📅 Prochaine Course                                    │
│  Run in Liège - 15/02/2025                            │
│                                                         │
└────────────────────────────────────────────────────────┘
```

#### 📝 Barre d'outils Quill

| Bouton | Fonction | Raccourci |
|--------|----------|-----------|
| **B** | Gras | Ctrl+B |
| **I** | Italique | Ctrl+I |
| **U** | Souligné | Ctrl+U |
| **Color** | Couleur du texte | - |
| **Background** | Couleur de fond | - |
| **Header** | Titres (H1, H2, H3) | - |
| **•** | Liste à puces | - |
| **1.** | Liste numérotée | - |
| **Align** | Alignement | - |
| **Link** | Insérer un lien | Ctrl+K |
| **Clean** | Supprimer le formatage | - |

### 3️⃣ Insérer un tableau

**Bouton spécial : "📊 Insert Table"**

1. Placez le curseur où vous voulez le tableau
2. Cliquez sur **"📊 Insert Table"**
3. Un tableau 4×4 est inséré automatiquement
4. Vous pouvez éditer les cellules directement

**Tableau par défaut:**
- 4 lignes × 4 colonnes
- En-tête orange
- Lignes alternées grises/blanches
- Bordures visibles

### 4️⃣ Sauvegarder

**Sauvegarde automatique:**
- Quand vous tapez ou modifiez
- Le HTML est automatiquement mis à jour dans le ViewModel

**Sauvegarde manuelle:**
- Cliquez sur **"💾 Get HTML"**

---

## 💡 Exemples d'édition

### Exemple 1 : Changer la couleur d'un titre

1. Sélectionnez le titre "Challenge 2025"
2. Cliquez sur le bouton **"Color"**
3. Choisissez une couleur (ex: bleu)

### Exemple 2 : Ajouter du texte en gras

1. Tapez votre texte
2. Sélectionnez-le
3. Cliquez sur **[B]** ou appuyez sur **Ctrl+B**

### Exemple 3 : Créer une liste

1. Tapez votre premier élément
2. Cliquez sur **[•]** (liste à puces)
3. Appuyez sur **Entrée** pour ajouter des éléments
4. Appuyez sur **Entrée** deux fois pour sortir de la liste

### Exemple 4 : Insérer un lien

1. Sélectionnez le texte
2. Cliquez sur l'icône **Link** ou appuyez sur **Ctrl+K**
3. Entrez l'URL
4. Cliquez sur **OK**

### Exemple 5 : Éditer un tableau

**Après avoir inséré un tableau:**

1. Cliquez dans une cellule
2. Tapez ou modifiez le texte
3. Utilisez **Tab** pour passer à la cellule suivante
4. Utilisez **Shift+Tab** pour revenir en arrière

**Pour modifier les cellules:**
- Double-cliquez pour éditer
- Sélectionnez et utilisez les boutons de formatage (gras, couleur, etc.)

---

## 🎨 Fonctionnalités Quill

### ✅ Formatage de texte
- **Gras, Italique, Souligné**
- **Couleur de texte** (16 couleurs prédéfinies)
- **Couleur de fond** (16 couleurs prédéfinies)
- **Supprimer le formatage** (bouton Clean)

### ✅ Structure
- **Titres** (H1, H2, H3, Normal)
- **Listes à puces**
- **Listes numérotées**
- **Alignement** (Gauche, Centre, Droite, Justifié)

### ✅ Éléments
- **Liens hypertexte**
- **Tableaux** (via bouton personnalisé)
- **Texte enrichi HTML**

### ✅ Édition
- **Annuler/Refaire** (Ctrl+Z / Ctrl+Y)
- **Copier-coller** depuis Word/Excel
- **Sélection de texte** intuitive

---

## 🔧 Architecture technique

### Composants

1. **Quill 1.3.7** (CDN)
   - Licence MIT (100% gratuit)
   - ~43 KB minifié
   - Aucune dépendance

2. **WebView2** (Microsoft Edge)
   - Rendu HTML5/CSS3
   - Communication C# ↔ JavaScript

3. **Communication**
   - JavaScript → C# : `WebMessageReceived`
   - C# → JavaScript : `ExecuteScriptAsync()`

### Flux de données

```
1. User clicks "Generate Template"
   ↓
2. C# génère le HTML
   ↓
3. User clicks "Load Template"
   ↓
4. C# envoie HTML à Quill (setContent)
   ↓
5. User édite dans Quill
   ↓
6. Quill envoie HTML à C# (contentChanged)
   ↓
7. C# met à jour le ViewModel
   ↓
8. Email prêt !
```

---

## 📦 Prérequis

### Edge WebView2 Runtime

✅ Généralement déjà installé sur Windows 10/11  
✅ Téléchargement : https://developer.microsoft.com/microsoft-edge/webview2/  
✅ Taille : ~150 MB

### Connexion Internet

⚠️ **Première utilisation uniquement**
- Quill est chargé depuis le CDN
- Une fois chargé, il est mis en cache
- Fonctionne ensuite hors connexion

💡 **Alternative**: Télécharger Quill localement (voir section ci-dessous)

---

## 🌐 Version locale (sans Internet)

### Pour utiliser Quill sans connexion Internet :

1. **Télécharger Quill**
   ```
   https://github.com/quilljs/quill/releases
   Télécharger quill.min.js et quill.snow.css
   ```

2. **Placer dans le projet**
   ```
   NameParser.UI\Resources\quill\
   ├── quill.min.js
   └── quill.snow.css
   ```

3. **Modifier QuillEditor.html**
   ```html
   <!-- Au lieu de CDN -->
   <link href="https://cdn.quilljs.com/1.3.7/quill.snow.css" rel="stylesheet">
   <script src="https://cdn.quilljs.com/1.3.7/quill.min.js"></script>
   
   <!-- Utiliser les fichiers locaux -->
   <link href="quill/quill.snow.css" rel="stylesheet">
   <script src="quill/quill.min.js"></script>
   ```

4. **Mettre à jour le .csproj**
   ```xml
   <None Update="Resources\quill\**\*">
     <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
   </None>
   ```

---

## ✅ Avantages par rapport à TinyMCE

| Critère | Quill | TinyMCE |
|---------|-------|---------|
| **Licence** | ✅ MIT (100% gratuit) | ⚠️ Clé API requise |
| **Taille** | ✅ 43 KB | ❌ 500+ KB |
| **Dépendances** | ✅ Aucune | ⚠️ Plusieurs |
| **Complexité** | ✅ Simple | ⚠️ Complexe |
| **Performance** | ✅ Rapide | ⚠️ Plus lent |
| **Open Source** | ✅ GitHub | ⚠️ Limité |
| **Hors ligne** | ✅ Facile | ⚠️ Difficile |

---

## 🐛 Dépannage

### Problème : L'éditeur ne se charge pas

**Solutions:**
1. Vérifiez votre connexion Internet (première utilisation)
2. Vérifiez que WebView2 Runtime est installé
3. Consultez le message d'erreur
4. Téléchargez Quill localement

### Problème : Le template ne se charge pas

**Solutions:**
1. Cliquez sur "Generate Email Template"
2. Attendez la génération
3. Cliquez sur "🔄 Load Template"
4. Vérifiez que l'éditeur est prêt

### Problème : Les tableaux ne s'affichent pas bien

**Solutions:**
1. Utilisez le bouton "📊 Insert Table"
2. Évitez de copier-coller des tableaux complexes depuis Word
3. Éditez les cellules manuellement

### Problème : Le formatage est perdu

**Solutions:**
1. Utilisez les boutons de la barre d'outils Quill
2. Évitez de coller du HTML complexe
3. Utilisez "Clean" pour réinitialiser le formatage

---

## 🚀 Limitations de Quill

### Ce que Quill ne fait PAS

❌ **Fusion de cellules dans les tableaux** - Non supporté nativement  
❌ **Images inline** - Supporté mais pas recommandé pour les emails  
❌ **Vérification orthographique** - Utilisez celle du navigateur (F7)  
❌ **Insertion de vidéos** - Non supporté  
❌ **Tableaux avancés** - Pas d'éditeur de tableau intégré

### Solutions de contournement

✅ **Tableaux**: Utilisez le bouton "Insert Table" qui génère du HTML compatible email  
✅ **Images**: Utilisez des URLs d'images hébergées  
✅ **Orthographe**: Le navigateur Edge intègre un correcteur orthographique  

---

## 📚 Ressources

- **Site officiel Quill**: https://quilljs.com/
- **Documentation**: https://quilljs.com/docs/
- **GitHub**: https://github.com/quilljs/quill
- **Playground**: https://quilljs.com/playground/

---

## ✅ Résumé

### Pour l'utilisateur

✅ Interface intuitive type Word  
✅ Aucune connaissance HTML requise  
✅ Édition WYSIWYG en temps réel  
✅ Sauvegarde automatique  
✅ Insertion de tableaux facilitée  

### Pour l'administrateur

✅ 100% gratuit et open source  
✅ Aucune clé API ou inscription  
✅ Léger et performant  
✅ HTML propre et compatible email  
✅ Facile à maintenir  

---

**Date**: 2025-02-09  
**Version**: 4.0 - Quill Editor (100% Free)  
**Build**: ✅ Réussi  
**Licence**: MIT (Open Source)  
**Prérequis**: Edge WebView2 Runtime + Internet (première utilisation)
