# 📝 Guide Rapide - Éditeur HTML pour Emails

## 🎯 Vue d'ensemble

L'onglet **Challenge Mailing** dispose d'un **éditeur HTML en split view** :
- **Panneau gauche** : Éditeur HTML (vous pouvez modifier le code)
- **Panneau droit** : Aperçu en temps réel (comme l'email final)

---

## ⚡ Démarrage rapide (3 étapes)

### 1. Générer le template
```
[Generate Email Template] ← Cliquez ici
```

### 2. Éditer le HTML
```
✏️ HTML Editor
┌────────────────────────┐
│ <h1>Challenge 2025</h1>│ ← Modifiez directement
│ <table>...</table>     │
└────────────────────────┘
```

### 3. Voir le résultat
```
👁️ Live Preview
┌────────────────────────┐
│ Challenge 2025         │ ← Aperçu en temps réel
│ [Tableau formaté]      │
└────────────────────────┘
```

---

## 🎨 Fonctionnalités principales

### ✅ Auto-refresh (Recommandé)
1. Cochez **☑️ "Auto-refresh on edit"**
2. Tapez dans l'éditeur HTML
3. L'aperçu se met à jour automatiquement après 0.8 seconde

### ✅ Update manuel
1. Modifiez le HTML
2. Cliquez sur **"🔄 Update Preview"**
3. Ou appuyez sur **Ctrl+U**

### ✅ Raccourcis clavier
- **Ctrl+U** ou **Ctrl+S** : Mettre à jour l'aperçu
- **Ctrl+Z** : Annuler
- **Ctrl+Y** : Refaire

---

## 📝 Modifications courantes

### Changer un texte
```html
<!-- Remplacez simplement le texte -->
<h1>Mon nouveau titre</h1>
```

### Ajouter une ligne dans un tableau
```html
<tr>
  <td>4</td>
  <td>Nouveau NOM</td>
  <td>50:00</td>
  <td>85</td>
</tr>
```

### Changer une couleur
```html
<!-- Orange → Bleu -->
<th style='background-color: #2196F3; color: white;'>
```

### Ajouter un message
```html
<p style='background-color: #FFE0B2; padding: 10px;'>
  ⚡ Message important !
</p>
```

---

## ⚠️ Points d'attention

### ✅ Bonnes pratiques
- Toujours fermer les balises : `<p>texte</p>`
- Utiliser des guillemets simples dans les styles : `style='...'`
- Tester avec "Send Test" avant l'envoi final

### ❌ À éviter
- Supprimer les styles des tableaux (ils sont essentiels)
- Oublier de fermer une balise
- Mettre du JavaScript (non supporté dans les emails)

---

## 🚀 Workflow recommandé

```
1. [Generate Template] 
   ↓
2. ☑️ Activer "Auto-refresh"
   ↓
3. Éditer le HTML à gauche
   ↓
4. Vérifier l'aperçu à droite
   ↓
5. [Send Test] à vous-même
   ↓
6. Comparer email reçu vs aperçu
   ↓
7. [Send to All Challengers]
```

---

## 💡 Astuces

### Astuce 1 : Copier un template
Sauvegardez votre HTML dans un fichier `.html` pour le réutiliser plus tard.

### Astuce 2 : Format du code
- Utilisez **Tab** pour indenter
- Gardez le code propre et lisible

### Astuce 3 : Tester d'abord
**Toujours** envoyer un email de test avant l'envoi massif !

---

## 🔧 Dépannage rapide

| Problème | Solution |
|----------|----------|
| L'aperçu ne se met pas à jour | Cliquez sur 🔄 ou Ctrl+U |
| Le HTML semble cassé | Vérifiez que toutes les balises sont fermées |
| Les couleurs ne s'affichent pas | Vérifiez la syntaxe CSS : `style='color: #FF9800;'` |

---

## ✅ Checklist avant envoi

- [ ] Le template a été généré
- [ ] Les modifications HTML sont faites
- [ ] L'aperçu correspond à mes attentes
- [ ] Les noms sont en MAJUSCULES
- [ ] Email de test envoyé et vérifié
- [ ] L'email reçu correspond à l'aperçu
- [ ] Prêt à envoyer à tous ! 🚀

---

**💡 Besoin d'aide ?** Consultez la documentation complète : `HTML_EDITOR_WITH_LIVE_PREVIEW.md`
