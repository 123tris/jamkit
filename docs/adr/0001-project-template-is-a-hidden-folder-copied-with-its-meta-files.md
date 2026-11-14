# The project template is a hidden folder copied with its .meta files

The project template ships inside the package as a hidden `Template~` folder that carries its `.meta` files, and scaffolding is a plain file copy of whatever the project does not already have. Copying the `.meta` files keeps every asset's GUID the same in every project, so references inside the template (a prefab to a variable, a scene to a prefab, a UI document to its data source) survive the copy with no rewiring. It replaces building the same assets in editor code, which restated every path and name and patched references by hand.

## Considered options

- **A visible folder in the package, copied through the AssetDatabase.** Editable and loadable in place, but copies get new GUIDs, so references between them need a remapping pass; Unity's documentation for `AssetDatabase.CopyAssets` promises none. Its assets would also appear in every project's searches, which breaks auto-assign's rule of filling a reference only when exactly one candidate exists.
- **Keep building in code and centralise the names.** The smallest change, but the prefabs, scenes and documents stay out of the editor's reach and the wiring stays code.

## Consequences

- The template cannot be opened or loaded in place. It is authored in a scaffolded project and exported back into the package.
- A template file's GUID is permanent. Regenerating one breaks every scaffolded project and anything authored against the template.
- Scaffolding never overwrites, and a project cannot hold a second copy of the template: the two would collide on GUIDs.
- The template is one fixed set of files, so it cannot vary with the optional integrations a project has installed. It is the baseline: Unity audio and no Feel wiring.
- The template's data is saved in the Ripple 0.3.0 Signal shape, so the JamKit release that ships it requires Ripple 0.3.0.
