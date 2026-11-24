# Dictionary data and pronunciation

The 100 learning cards and their additional contextual senses are curated for Lilt. General word lookup uses a compact offline adaptation of the following datasets. Dictionary data is separate from the application source code and character artwork.

## Wordset / WordNet

- Source: https://github.com/wordset/wordset-dictionary
- Snapshot: `allwords_wordset.json.gz`, downloaded 2026-11-24.
- License: Creative Commons Attribution-ShareAlike 4.0 International, with the included WordNet 3.0 notices.
- Full upstream notices: [licenses/Wordset-WordNet.txt](licenses/Wordset-WordNet.txt).
- Changes: lowercase lookup keys, selection of up to two senses per part of speech, removal of unused metadata, compression and indexing by initial letter, plus explicitly identified Lilt technical entries. The adapted dictionary data in `assets/dictionary.bin` is distributed under CC BY-SA 4.0. The generator and original source references provide the corresponding editable form.
- License text: https://creativecommons.org/licenses/by-sa/4.0/legalcode

## CMU Pronouncing Dictionary

- Source: https://github.com/cmusphinx/cmudict
- Snapshot: `cmudict.dict`, downloaded 2026-11-24.
- Full copyright, redistribution terms and disclaimer: [licenses/CMUdict.txt](licenses/CMUdict.txt).
- Changes: selection of a pronunciation and broad American IPA conversion from ARPABET for general lookup. Curated learning-card IPA remains separately specified.

## Optional online lookup

If an offline entry is unavailable, an explicit “Look up online” action requests that single word from https://dictionaryapi.dev/. There are no automatic network requests during ordinary learning. Returned dictionary content is attributed to Free Dictionary API / Wiktionary; its attribution appears in the word view. Only the looked-up word is sent, not the surrounding page or learning history.

## Character icon

The supplied Suzuha Amane image was edited with the built-in imagegen tool to remove the exterior background. The prompt requested alpha transparency while preserving the character, colors, pixel-art silhouette and internal highlights, with no added outline, badge, text or shadow. Character and artwork rights remain with their respective holders.
