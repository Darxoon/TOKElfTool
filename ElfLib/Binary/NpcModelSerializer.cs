using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using ElfLib.Types.Registry;

namespace ElfLib
{
    public class NpcModelSerializer<T> : ElfSerializer<T>
    {
        private Dictionary<string, SectionPointer> stringDeclarationMap;
        
        protected override (byte[] data, byte[] rodata, byte[] relaRodata) SerializeData(
            Dictionary<ElfType, List<Element<T>>> data, 
            Dictionary<ElfType, List<long>> dataOffsets
        ) {
            stringDeclarationMap = new Dictionary<string, SectionPointer>();

            using MemoryStream stringSectionStream = new MemoryStream();
            using BinaryWriter stringSectionWriter = new BinaryWriter(stringSectionStream);
            
            
            
            
            // Prepare list of all strings for .data
            HashSet<string> dataStrings = new HashSet<string>();

            foreach (Element<T> element in data[ElfType.Main])
            {
                if (element.value is NpcModel model)
                    dataStrings.Add(model.model_id);
                // TODO: remove this
                else
                    throw new Exception();
            }

            SerializeStrings(stringSectionWriter, dataStrings, stringDeclarationMap);

            // TODO: Convert objects to raw objects
            List<object> rawObjects = new List<object>();
            dataRelocationMap = new SortedDictionary<long, SectionPointer>();
            
            long dataSectionPosition = 0;
            ConvertToRawObjects(data[ElfType.Main], GameDataType.DataNpcModel, stringDeclarationMap, rawObjects, dataRelocationMap, ref dataSectionPosition);
            
            // Serialize them
            using MemoryStream dataStream = new MemoryStream();
            using BinaryWriter dataWriter = new BinaryWriter(dataStream);
            
            foreach (var item in rawObjects)
            {
                Util.ToBinaryWriter(dataWriter, item);
            }
            
            
            
            // strings in .rodata are serialized in this order:
            // model_files object, state object, all anime objects connected to this state,
            // model_files object, state object, all anime objects connected to this state, etc..
            
            // even though it might seem like the order doesn't matter, it is important to me that the input file and unmodified
            // output file for data_npc_model.elf are equal by hash, in order to not break things that would go unnoticed otherwise
            
            // this is why this will go through all the files and state objects, collect which anime objects are related to that, 
            // then go through the files and state objects again, serializing the strings AND the anime's strings in the process
            
            // Prepare model files and state objects
            var basicRodataObjects = new List<(Element<T> instance, long offset, ElfType type)>(data[ElfType.Files].Count + data[ElfType.State].Count);
            basicRodataObjects.AddRange(data[ElfType.Files].Zip(dataOffsets[ElfType.Files], (element, l) => (element, l, ElfType.Files)));
            basicRodataObjects.AddRange(data[ElfType.State].Zip(dataOffsets[ElfType.State], (element, l) => (element, l, ElfType.State)));
            basicRodataObjects.Sort((x, y) => x.offset.CompareTo(y.offset));

            List<int> firstAnimeIndices = new List<int>(basicRodataObjects.Count / 2);
            
            foreach ((Element<T> instance, long offset, ElfType type) in basicRodataObjects)
            {
                if (!(instance.value is List<object> list) || list.Count == 0)
                    continue;


                if (list[0] is NpcModelState state)
                {
                    NpcModelSubState firstSubState = (NpcModelSubState)(
                        (List<object>)(object)data[ElfType.SubStates][dataOffsets[ElfType.SubStates].IndexOf(state.substate_arr.AsLong)].value
                    )[0];
                    NpcModelFace firstFace = (NpcModelFace)(
                        (List<object>)(object)data[ElfType.Face][dataOffsets[ElfType.Face].IndexOf(firstSubState.face_arr.AsLong)].value
                    )[0];
                    int firstAnimeIndex = dataOffsets[ElfType.Anime].IndexOf(firstFace.anime_arr.AsLong);
                    firstAnimeIndices.Add(firstAnimeIndex);
                }
            }

            firstAnimeIndices.Add(data[ElfType.Anime].Count);
            
            // Add all strings from all model_files, state and anime objects for a type
            HashSet<string> rodataStrings = new HashSet<string>();

            int firstAnimeIndexIterator = 0;
            int animeIterator = 0;

            for (int i = 0; i < basicRodataObjects.Count; i++)
            {
                (Element<T> instance, long offset, ElfType type) = basicRodataObjects[i];
                
                if (!(instance.value is List<object> list) || list.Count == 0)
                    continue;

                for (int j = 0; j < list.Count; j++)
                {
                    switch (list[j])
                    {
                        case NpcModelFiles files:
                            rodataStrings.Add(files.model_folder);
                            rodataStrings.Add(files.model_name);
                            rodataStrings.Add(files.field_0x10);
                            rodataStrings.Add(files.field_0x18);
                            break;
                        case NpcModelState state:
                            rodataStrings.Add(state.description);
                            break;
                    }
                }

                if (list[0] is NpcModelState)
                {
                    // Add anime strings
                    while (animeIterator < firstAnimeIndices[firstAnimeIndexIterator + 1])
                    {
                        foreach (NpcModelAnime anime in (List<object>)(object)data[ElfType.Anime][animeIterator].value)
                        {
                            rodataStrings.Add(anime.description);
                            rodataStrings.Add(anime.id);
                        }
                        animeIterator++;
                    }

                    firstAnimeIndexIterator++;
                }
            }
            
            SerializeStrings(stringSectionWriter, rodataStrings, stringDeclarationMap);
            
            
            // Begin serializing
            using MemoryStream rodataStream = new MemoryStream();
            using BinaryWriter rodataWriter = new BinaryWriter(rodataStream);
            
            // Convert model files and state to raw objects and serialize
            SortedDictionary<long, SectionPointer> rodataRelocationMap = new SortedDictionary<long, SectionPointer>();

            for (int i = 0; i < basicRodataObjects.Count - 2; i++)
            {
                List<object> list = (List<object>)(object)basicRodataObjects[i].instance.value;

                for (int j = 0; j < list.Count; j++)
                {
                    object rawInstance = list[j] switch
                    {
                        NpcModelFiles files => (object)Util.NormalToRawObject<RawNpcModelFiles, NpcModelFiles>(files,
                            stringDeclarationMap, rodataRelocationMap, rodataStream.Position),
                        NpcModelState state => (object)Util.NormalToRawObject<RawNpcModelState, NpcModelState>(state,
                            stringDeclarationMap, rodataRelocationMap, rodataStream.Position),
                    };
                
                    Util.ToBinaryWriter(rodataWriter, rawInstance);
                }
                
                Util.ToBinaryWriter(rodataWriter, basicRodataObjects[i].type switch {
                    ElfType.Files => (object)new RawNpcModelFiles(),
                    ElfType.State => (object)new RawNpcModelState(),
                });
            }
            
            // modelNpc_num (amount of entries in .data)
            rodataWriter.Write((long)data[ElfType.Main].Count - 1);
            
            // Serialize the sub sections for all entries
            foreach (Element<T> element in data[ElfType.State])
            {
                // Serialize the sub states and collect references to
                List<List<Pointer>> faceArrays = new List<List<Pointer>>();
                
                List<object> states = (List<object>)(object)element.value;
                
                foreach (NpcModelState state in states)
                {
                    List<object> subStates = (List<object>)(object)data[ElfType.SubStates][dataOffsets[ElfType.SubStates].IndexOf(state.substate_arr.AsLong)].value;
                    List<Pointer> localFaceArrays = new List<Pointer>();

                    foreach (NpcModelSubState subState in subStates)
                    {
                        localFaceArrays.Add(subState.face_arr);
                        
                        RawNpcModelSubState rawSubState = Util.NormalToRawObject<RawNpcModelSubState, NpcModelSubState>(subState,
                            stringDeclarationMap, rodataRelocationMap, rodataStream.Position);
                        
                        Util.ToBinaryWriter(rodataWriter, rawSubState);
                    }
                    
                    faceArrays.Add(localFaceArrays);
                    
                    Util.ToBinaryWriter(rodataWriter, new RawNpcModelSubState());

                }

                // Serialize the faces and animations
                foreach (List<Pointer> localFaceArrays in faceArrays)
                {
                    List<Pointer> animeArrays = new List<Pointer>();
                    
                    foreach (Pointer facePointer in localFaceArrays)
                    {
                        List<object> faces = (List<object>)(object)data[ElfType.Face][dataOffsets[ElfType.Face].IndexOf(facePointer.AsLong)].value;
                    
                        foreach (NpcModelFace face in faces)
                        {
                            animeArrays.Add(face.anime_arr);
                        
                            RawNpcModelFace rawFace = Util.NormalToRawObject<RawNpcModelFace, NpcModelFace>(face,
                                stringDeclarationMap, rodataRelocationMap, rodataStream.Position);
                        
                            Util.ToBinaryWriter(rodataWriter, rawFace);
                        }
                        
                        Util.ToBinaryWriter(rodataWriter, new RawNpcModelFace());
                    }
                    
                    foreach (Pointer animePointer in animeArrays)
                    {
                        List<object> animes = (List<object>)(object)data[ElfType.Anime][dataOffsets[ElfType.Anime].IndexOf(animePointer.AsLong)].value;
                    
                        foreach (NpcModelAnime anime in animes)
                        {
                            RawNpcModelAnime rawAnime = Util.NormalToRawObject<RawNpcModelAnime, NpcModelAnime>(anime,
                                stringDeclarationMap, rodataRelocationMap, rodataStream.Position);
                        
                            Util.ToBinaryWriter(rodataWriter, rawAnime);
                        }
                        
                        Util.ToBinaryWriter(rodataWriter, new RawNpcModelAnime());
                    }
                }
                
                // foreach (Pointer facePointer in faceArrays)
                // {
                //     List<Pointer> animeArrays = new List<Pointer>();
                //     
                //     List<object> faces = (List<object>)(object)data[ElfType.Face][dataOffsets[ElfType.Face].IndexOf(facePointer.AsLong)].value;
                //     
                //     foreach (NpcModelFace face in faces)
                //     {
                //         animeArrays.Add(face.anime_arr);
                //         
                //         RawNpcModelFace rawFace = Util.NormalToRawObject<RawNpcModelFace, NpcModelFace>(face,
                //             stringDeclarationMap, rodataRelocationMap, rodataStream.Position);
                //         
                //         Util.ToBinaryWriter(rodataWriter, rawFace);
                //     }
                //     
                //     Util.ToBinaryWriter(rodataWriter, new RawNpcModelFace());
                //     
                //     // Serialize animes
                //     foreach (Pointer animePointer in animeArrays)
                //     {
                //         List<object> animes = (List<object>)(object)data[ElfType.Anime][dataOffsets[ElfType.Anime].IndexOf(animePointer.AsLong)].value;
                //     
                //         foreach (NpcModelAnime anime in animes)
                //         {
                //             RawNpcModelAnime rawAnime = Util.NormalToRawObject<RawNpcModelAnime, NpcModelAnime>(anime,
                //                 stringDeclarationMap, rodataRelocationMap, rodataStream.Position);
                //         
                //             Util.ToBinaryWriter(rodataWriter, rawAnime);
                //         }
                //         
                //         Util.ToBinaryWriter(rodataWriter, new RawNpcModelAnime());
                //     }
                // }
            }
            
            stringSectionData = stringSectionStream.ToArray();
            
            return (dataStream.ToArray(), rodataStream.ToArray(), SerializeRelaData(rodataRelocationMap, GameDataType.DataNpcModel));
        }
        

        private void SerializeStrings(BinaryWriter writer, IEnumerable<string> strings, Dictionary<string, SectionPointer> declarationMap)
        {
            foreach (string str in strings)
            {
                if (str == null || declarationMap.ContainsKey(str))
                    continue;
                
                // Add entry to dict
                declarationMap.Add(str, new SectionPointer(writer.BaseStream.Position, 0x600000101));

                // Create char[]
                char[] chars = new char[str.Length + 1];
                for (int i = 0; i < str.Length; i++)
                {
                    chars[i] = str[i];
                }
                
                chars[chars.Length - 1] = '\0';
                
                // Write char array
                writer.Write(chars);
            }
        }
    }
}