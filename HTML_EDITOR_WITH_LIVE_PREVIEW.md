# ✅ Éditeur HTML avec Aperçu en Temps Réel

## 📋 Vue d'ensemble

L'onglet **Challenge Mailing** dispose maintenant d'un **éditeur HTML complet** avec aperçu en temps réel, permettant d'éditer le contenu de l'email avant de l'envoyer.

---

## 🎯 Fonctionnalités

### ✅ Ce que vous pouvez faire

1. **Générer un template HTML automatiquement**
   - Cliquez sur "Generate Email Template"
   - Le HTML est généré et s'affiche dans l'éditeur

2. **Éditer le HTML directement**
   - Modifier le texte, les tableaux, les styles
   - Ajouter ou supprimer des sections
   - Personnaliser complètement l'email

3. **Voir l'aperçu en temps réel**
   - Option "Auto-refresh" pour mise à jour automatique
   - Ou bouton "Update Preview" pour mise à jour manuelle
   - L'aperçu est identique à l'email final

4. **Raccourcis clavier**
   - **Ctrl+U** : Mettre à jour l'aperçu
   - **Ctrl+S** : Sauvegarder et mettre à jour

---

## 🎨 Interface divisée (Split View)

```
┌────────────────────────────────────────────────────────────────┐
│                    Challenge Mailing                           │
├────────────────────────────────────────────────────────────────┤
│  [Generate Email Template]                                     │
│  Subject: [_________________________________________]           │
│                                                                 │
│  ┌──────────────────────────┬──────────────────────────┐      │
│  │ ✏️ HTML Editor           │ 👁️ Live Preview          │      │
│  │ [🔄 Update Preview]      │ ☑️ Auto-refresh on edit  │      │
│  ├──────────────────────────┼──────────────────────────┤      │
│  │                          │                          │      │
│  │ <h1 style='color:        │  🏃 Challenge 2025       │      │
│  │   #FF9800;'>             │  ───────────────────     │      │
│  │   🏃 Challenge 2025      │                          │      │
│  │ </h1>                    │  📅 Prochaine Course     │      │
│  │                          │  Run in Liège            │      │
│  │ <table>                  │                          │      │
│  │   <tr>                   │  🏆 Derniers Résultats   │      │
│  │     <th>Pos</th>         │  ┌───┬───────────┬────┐  │      │
│  │     <th>Nom</th>         │  │Pos│Nom        │Time│  │      │
│  │   </tr>                  │  ├───┼───────────┼────┤  │      │
│  │   <tr>                   │  │1  │Jean DUPONT│45mn│  │      │
│  │     <td>1</td>           │  └───┴───────────┴────┘  │      │
│  │     <td>Jean DUPONT</td> │                          │      │
│  │   </tr>                  │                          │      │
│  │ </table>                 │                          │      │
│  │                          │                          │      │
│  └──────────────────────────┴──────────────────────────┘      │
│                                                                 │
│  Test Email: [test@example.com] [Send Test]                   │
│  [Send to All Challengers]                                     │
└────────────────────────────────────────────────────────────────┘
```

---

## 🚀 Guide d'utilisation

### 1️⃣ Générer le template initial

1. Sélectionnez un **challenge** dans la liste déroulante
2. Cliquez sur **"Generate Email Template"**
3. Le HTML s'affiche automatiquement dans l'éditeur (panneau gauche)
4. L'aperçu s'affiche automatiquement (panneau droit)

### 2️⃣ Éditer le HTML

**Dans le panneau gauche (HTML Editor):**

```html
<!-- Vous pouvez modifier n'importe quoi -->
<h1 style='color: #FF9800;'>🏃 Challenge 2025</h1>
<p>Mise à jour du Challenge - 09 février 2025</p>

<!-- Modifier les tableaux -->
<table>
  <tr style='background-color: #FF9800; color: white;'>
    <th>Position</th>
    <th>Nom</th>
    <th>Temps</th>
  </tr>
  <tr>
    <td>1</td>
    <td>Jean DUPONT</td>
    <td>00:45:23</td>
  </tr>
</table>
```

### 3️⃣ Mettre à jour l'aperçu

**Option A: Automatique**
1. Cochez **"☑️ Auto-refresh on edit"**
2. L'aperçu se met à jour automatiquement pendant que vous tapez
3. Délai de 800ms après la dernière frappe

**Option B: Manuel**
1. Modifiez le HTML
2. Cliquez sur **"🔄 Update Preview"**
3. Ou utilisez **Ctrl+U** ou **Ctrl+S**

### 4️⃣ Personnalisation avancée

**Changer les couleurs:**
```html
<!-- Orange → Bleu -->
<th style='background-color: #2196F3; color: white;'>Nom</th>
```

**Ajouter du texte:**
```html
<p style='font-size: 14px; color: #666;'>
  Nouveau message personnalisé ici !
</p>
```

**Ajouter une ligne dans le tableau:**
```html
<tr>
  <td>3</td>
  <td>Pierre MARTIN</td>
  <td>00:47:15</td>
  <td>90</td>
</tr>
```

**Supprimer une section:**
- Supprimez simplement le code HTML correspondant
- Mise à jour automatique ou manuelle

### 5️⃣ Envoyer l'email

Une fois satisfait de l'édition:

1. **Test Email**: Envoyez un test à vous-même
2. **Send to All Challengers**: Envoyez à tous les participants

---

## 💡 Astuces d'édition

### ✨ Astuce 1: Coloration syntaxique
L'éditeur utilise une **police monospace** (Consolas) pour faciliter la lecture du code HTML.

### ✨ Astuce 2: Auto-refresh intelligent
L'auto-refresh attend **800ms** après votre dernière frappe pour éviter les mises à jour trop fréquentes.

### ✨ Astuce 3: Indentation
Utilisez la **touche Tab** pour indenter votre code HTML et le rendre plus lisible.

### ✨ Astuce 4: Copier-coller
Vous pouvez copier du HTML depuis n'importe où et le coller dans l'éditeur.

### ✨ Astuce 5: Annuler/Refaire
**Ctrl+Z** pour annuler, **Ctrl+Y** pour refaire (fonctionnalités natives du TextBox).

---

## 🎨 Exemples de personnalisation

### Exemple 1: Changer la couleur principale

**Avant (Orange):**
```html
<h1 style='color: #FF9800;'>🏃 Challenge 2025</h1>
<tr style='background-color: #FF9800; color: white;'>
```

**Après (Vert):**
```html
<h1 style='color: #4CAF50;'>🏃 Challenge 2025</h1>
<tr style='background-color: #4CAF50; color: white;'>
```

### Exemple 2: Ajouter un message personnalisé

**Insérer après le titre:**
```html
<h1 style='color: #FF9800;'>🏃 Challenge 2025</h1>

<!-- Nouveau message -->
<div style='background-color: #FFE0B2; padding: 15px; border-radius: 5px; margin: 10px 0;'>
  <p style='font-weight: bold; color: #E65100;'>
    ⚡ NOUVEAU: Inscriptions ouvertes pour la prochaine course !
  </p>
  <p>Ne manquez pas cette opportunité. Places limitées !</p>
</div>

<p style='font-size: 14px; color: #666;'>Mise à jour du Challenge...</p>
```

### Exemple 3: Modifier le footer

**Avant:**
```html
<p style='font-size: 12px; color: #666;'>
  Continuez le beau travail ! À bientôt à la prochaine course ! 🏃💪
</p>
```

**Après:**
```html
<hr style='border: 1px solid #FF9800; margin-top: 30px;'/>
<p style='font-size: 12px; color: #666;'>
  Merci de votre participation au Challenge 2025 ! 🏃
</p>
<p style='font-size: 11px; color: #999;'>
  Questions ? Contactez-nous à challenge@example.com
</p>
```

---

## ⚙️ Raccourcis clavier

| Raccourci | Action |
|-----------|--------|
| **Ctrl+U** | Mettre à jour l'aperçu |
| **Ctrl+S** | Sauvegarder et mettre à jour |
| **Ctrl+Z** | Annuler |
| **Ctrl+Y** | Refaire |
| **Ctrl+A** | Tout sélectionner |
| **Ctrl+F** | Rechercher (natif Windows) |
| **Tab** | Indenter |
| **Shift+Tab** | Désindenter |

---

## 🔒 Validation HTML

### ⚠️ Points d'attention

1. **Balises fermées**: Assurez-vous que toutes les balises sont fermées
   - ✅ `<p>Texte</p>`
   - ❌ `<p>Texte`

2. **Guillemets**: Utilisez des guillemets simples dans les styles
   - ✅ `style='color: #FF9800;'`
   - ⚠️ `style="color: #FF9800;"` (peut causer des problèmes)

3. **Caractères spéciaux**: Utilisez les entités HTML si nécessaire
   - `&lt;` pour `<`
   - `&gt;` pour `>`
   - `&amp;` pour `&`

4. **Structure des tableaux**: Respectez la hiérarchie
   ```html
   <table>
     <thead>
       <tr>
         <th>Colonne</th>
       </tr>
     </thead>
     <tbody>
       <tr>
         <td>Donnée</td>
       </tr>
     </tbody>
   </table>
   ```

---

## 🐛 Dépannage

### Problème: L'aperçu ne se met pas à jour

**Solutions:**
1. Cliquez sur "🔄 Update Preview"
2. Ou utilisez **Ctrl+U**
3. Vérifiez que la checkbox "Auto-refresh" est cochée si vous voulez l'auto-refresh

### Problème: Le HTML n'est pas bien formaté

**Solutions:**
1. Utilisez un formateur HTML en ligne (ex: https://htmlformatter.com/)
2. Copiez votre HTML
3. Formatez-le
4. Collez-le de nouveau dans l'éditeur

### Problème: Les couleurs ne s'affichent pas

**Solutions:**
1. Vérifiez la syntaxe: `style='color: #FF9800;'`
2. Utilisez des couleurs hexadécimales ou des noms de couleur
3. Assurez-vous que les guillemets sont corrects

### Problème: Les tableaux sont décalés

**Solutions:**
1. Vérifiez que toutes les balises `<tr>` ont le même nombre de `<td>`
2. Utilisez `colspan` pour fusionner des colonnes si nécessaire
3. Vérifiez la fermeture des balises

---

## 📊 Comparaison des modes

| Fonctionnalité | Mode Auto-refresh ON | Mode Auto-refresh OFF |
|----------------|---------------------|----------------------|
| **Mise à jour** | Automatique (800ms) | Manuelle (bouton ou Ctrl+U) |
| **Performance** | ⚠️ Légèrement plus lent | ✅ Optimal |
| **Usage** | 📝 Édition active | 📝 Édition ponctuelle |
| **Recommandé pour** | Petites modifications | Gros changements HTML |

---

## 🎯 Bonnes pratiques

### ✅ À faire

1. ✅ **Tester avant d'envoyer**: Toujours envoyer un email de test
2. ✅ **Sauvegarder le HTML**: Copier le HTML dans un fichier si modifications importantes
3. ✅ **Utiliser l'auto-refresh**: Pour voir immédiatement vos changements
4. ✅ **Vérifier l'aperçu**: S'assurer que l'aperçu correspond à vos attentes
5. ✅ **Conserver le template de base**: Ne pas supprimer les styles essentiels

### ❌ À éviter

1. ❌ **Ne pas fermer les balises**: Cause des problèmes d'affichage
2. ❌ **HTML trop complexe**: Les clients emails ne supportent pas tout
3. ❌ **JavaScript**: Non supporté dans les emails
4. ❌ **Images non hébergées**: Utiliser des URLs complètes pour les images
5. ❌ **Supprimer les styles de base**: Conservez au moins les styles des tableaux

---

## 🔄 Workflow recommandé

1. **Générer** le template automatique
2. **Éditer** le HTML dans le panneau gauche
3. **Vérifier** l'aperçu dans le panneau droit
4. **Tester** avec "Send Test"
5. **Comparer** l'email reçu avec l'aperçu
6. **Ajuster** si nécessaire
7. **Envoyer** à tous les challengers

---

## 📚 Ressources HTML utiles

### Balises HTML de base pour emails

- `<h1>` à `<h6>` : Titres
- `<p>` : Paragraphe
- `<strong>` ou `<b>` : Gras
- `<em>` ou `<i>` : Italique
- `<br>` : Saut de ligne
- `<hr>` : Ligne horizontale
- `<table>`, `<tr>`, `<td>`, `<th>` : Tableaux
- `<div>` : Conteneur
- `<span>` : Conteneur inline

### Styles CSS inline

```html
<!-- Couleur de texte -->
<p style='color: #FF9800;'>Texte orange</p>

<!-- Couleur de fond -->
<div style='background-color: #E3F2FD;'>Fond bleu clair</div>

<!-- Taille de police -->
<p style='font-size: 14px;'>Texte 14px</p>

<!-- Espacement -->
<div style='padding: 10px; margin: 5px;'>Avec espacement</div>

<!-- Bordure -->
<div style='border: 1px solid #CCCCCC;'>Avec bordure</div>
```

---

**Date de mise en œuvre**: 2025-02-09  
**Version**: 2.0 - Éditeur HTML complet  
**Build**: ✅ Réussi
