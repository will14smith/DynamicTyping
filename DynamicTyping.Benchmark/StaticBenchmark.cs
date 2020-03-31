using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Utf8Json;

namespace DynamicTyping.Benchmark
{
    class StaticBenchmark
    {
        private readonly Static _instance;
        private readonly IResolver _resolver;
        
        public StaticBenchmark()
        {
            _instance = JsonSerializer.Deserialize<Static>(Program.InputBytes);
            SetParents(_instance);
            
            _resolver = new StaticResolver();
        }

        public Static Read()
        {
            var a = JsonSerializer.Deserialize<Static>(Program.InputBytes);
            SetParents(a);
            return a;
        }
        public string Write()
        {
            return JsonSerializer.ToJsonString(_instance);
        }
        public ResolveResult[] Resolve()
        {
            return new[]
            {
                _resolver.Resolve(_instance, "Id"),
                _resolver.Resolve(_instance, "DecimalProp7"),
                _resolver.Resolve(_instance, "StringProperty98"),
                _resolver.Resolve(_instance, "NotAProperty"),
            };
        }
        
        private static void SetParents(Static instance)
        {
            var children = instance.Children;
            if (children == null) return;

            foreach (var child in children)
            {
                if (child == null) continue;

                child.Parent = instance;
                SetParents(child);
            }
        }
    }
    
    
    class StaticResolver : IResolver
    {
        public ResolveResult Resolve(object obj, string field)
        {
            if (!(obj is Static instance)) return ResolveResult.Unresolved;

            return field switch
            {
                nameof(Static.Id) => ResolveResult.Resolve(instance.Id),
                nameof(Static.Name) => ResolveResult.Resolve(instance.Name),
                nameof(Static.DecimalProp1) => ResolveResult.Resolve(instance.DecimalProp1),
                nameof(Static.DecimalProp2) => ResolveResult.Resolve(instance.DecimalProp2),
                nameof(Static.DecimalProp3) => ResolveResult.Resolve(instance.DecimalProp3),
                nameof(Static.DecimalProp4) => ResolveResult.Resolve(instance.DecimalProp4),
                nameof(Static.DecimalProp5) => ResolveResult.Resolve(instance.DecimalProp5),
                nameof(Static.DecimalProp6) => ResolveResult.Resolve(instance.DecimalProp6),
                nameof(Static.DecimalProp7) => ResolveResult.Resolve(instance.DecimalProp7),
                nameof(Static.DecimalProp8) => ResolveResult.Resolve(instance.DecimalProp8),
                nameof(Static.DecimalProp9) => ResolveResult.Resolve(instance.DecimalProp9),
                nameof(Static.DecimalProp10) => ResolveResult.Resolve(instance.DecimalProp10),
                nameof(Static.DecimalProp11) => ResolveResult.Resolve(instance.DecimalProp11),
                nameof(Static.DecimalProp12) => ResolveResult.Resolve(instance.DecimalProp12),
                nameof(Static.DecimalProp13) => ResolveResult.Resolve(instance.DecimalProp13),
                nameof(Static.DecimalProp14) => ResolveResult.Resolve(instance.DecimalProp14),
                nameof(Static.DecimalProp15) => ResolveResult.Resolve(instance.DecimalProp15),
                nameof(Static.DecimalProp16) => ResolveResult.Resolve(instance.DecimalProp16),
                nameof(Static.DecimalProp17) => ResolveResult.Resolve(instance.DecimalProp17),
                nameof(Static.DecimalProp18) => ResolveResult.Resolve(instance.DecimalProp18),
                nameof(Static.DecimalProp19) => ResolveResult.Resolve(instance.DecimalProp19),
                nameof(Static.DecimalProp20) => ResolveResult.Resolve(instance.DecimalProp20),
                nameof(Static.DecimalProp21) => ResolveResult.Resolve(instance.DecimalProp21),
                nameof(Static.DecimalProp22) => ResolveResult.Resolve(instance.DecimalProp22),
                nameof(Static.DecimalProp23) => ResolveResult.Resolve(instance.DecimalProp23),
                nameof(Static.DecimalProp24) => ResolveResult.Resolve(instance.DecimalProp24),
                nameof(Static.DecimalProp25) => ResolveResult.Resolve(instance.DecimalProp25),
                nameof(Static.DecimalProp26) => ResolveResult.Resolve(instance.DecimalProp26),
                nameof(Static.DecimalProp27) => ResolveResult.Resolve(instance.DecimalProp27),
                nameof(Static.DecimalProp28) => ResolveResult.Resolve(instance.DecimalProp28),
                nameof(Static.DecimalProp29) => ResolveResult.Resolve(instance.DecimalProp29),
                nameof(Static.DecimalProp30) => ResolveResult.Resolve(instance.DecimalProp30),
                nameof(Static.DecimalProp31) => ResolveResult.Resolve(instance.DecimalProp31),
                nameof(Static.DecimalProp32) => ResolveResult.Resolve(instance.DecimalProp32),
                nameof(Static.DecimalProp33) => ResolveResult.Resolve(instance.DecimalProp33),
                nameof(Static.DecimalProp34) => ResolveResult.Resolve(instance.DecimalProp34),
                nameof(Static.DecimalProp35) => ResolveResult.Resolve(instance.DecimalProp35),
                nameof(Static.DecimalProp36) => ResolveResult.Resolve(instance.DecimalProp36),
                nameof(Static.DecimalProp37) => ResolveResult.Resolve(instance.DecimalProp37),
                nameof(Static.DecimalProp38) => ResolveResult.Resolve(instance.DecimalProp38),
                nameof(Static.DecimalProp39) => ResolveResult.Resolve(instance.DecimalProp39),
                nameof(Static.DecimalProp40) => ResolveResult.Resolve(instance.DecimalProp40),
                nameof(Static.DecimalProp41) => ResolveResult.Resolve(instance.DecimalProp41),
                nameof(Static.DecimalProp42) => ResolveResult.Resolve(instance.DecimalProp42),
                nameof(Static.DecimalProp43) => ResolveResult.Resolve(instance.DecimalProp43),
                nameof(Static.DecimalProp44) => ResolveResult.Resolve(instance.DecimalProp44),
                nameof(Static.DecimalProp45) => ResolveResult.Resolve(instance.DecimalProp45),
                nameof(Static.DecimalProp46) => ResolveResult.Resolve(instance.DecimalProp46),
                nameof(Static.DecimalProp47) => ResolveResult.Resolve(instance.DecimalProp47),
                nameof(Static.DecimalProp48) => ResolveResult.Resolve(instance.DecimalProp48),
                nameof(Static.DecimalProp49) => ResolveResult.Resolve(instance.DecimalProp49),
                nameof(Static.DecimalProp50) => ResolveResult.Resolve(instance.DecimalProp50),
                nameof(Static.DecimalProp51) => ResolveResult.Resolve(instance.DecimalProp51),
                nameof(Static.DecimalProp52) => ResolveResult.Resolve(instance.DecimalProp52),
                nameof(Static.DecimalProp53) => ResolveResult.Resolve(instance.DecimalProp53),
                nameof(Static.DecimalProp54) => ResolveResult.Resolve(instance.DecimalProp54),
                nameof(Static.DecimalProp55) => ResolveResult.Resolve(instance.DecimalProp55),
                nameof(Static.DecimalProp56) => ResolveResult.Resolve(instance.DecimalProp56),
                nameof(Static.DecimalProp57) => ResolveResult.Resolve(instance.DecimalProp57),
                nameof(Static.DecimalProp58) => ResolveResult.Resolve(instance.DecimalProp58),
                nameof(Static.DecimalProp59) => ResolveResult.Resolve(instance.DecimalProp59),
                nameof(Static.DecimalProp60) => ResolveResult.Resolve(instance.DecimalProp60),
                nameof(Static.DecimalProp61) => ResolveResult.Resolve(instance.DecimalProp61),
                nameof(Static.DecimalProp62) => ResolveResult.Resolve(instance.DecimalProp62),
                nameof(Static.DecimalProp63) => ResolveResult.Resolve(instance.DecimalProp63),
                nameof(Static.DecimalProp64) => ResolveResult.Resolve(instance.DecimalProp64),
                nameof(Static.DecimalProp65) => ResolveResult.Resolve(instance.DecimalProp65),
                nameof(Static.DecimalProp66) => ResolveResult.Resolve(instance.DecimalProp66),
                nameof(Static.DecimalProp67) => ResolveResult.Resolve(instance.DecimalProp67),
                nameof(Static.DecimalProp68) => ResolveResult.Resolve(instance.DecimalProp68),
                nameof(Static.DecimalProp69) => ResolveResult.Resolve(instance.DecimalProp69),
                nameof(Static.DecimalProp70) => ResolveResult.Resolve(instance.DecimalProp70),
                nameof(Static.DecimalProp71) => ResolveResult.Resolve(instance.DecimalProp71),
                nameof(Static.DecimalProp72) => ResolveResult.Resolve(instance.DecimalProp72),
                nameof(Static.DecimalProp73) => ResolveResult.Resolve(instance.DecimalProp73),
                nameof(Static.DecimalProp74) => ResolveResult.Resolve(instance.DecimalProp74),
                nameof(Static.DecimalProp75) => ResolveResult.Resolve(instance.DecimalProp75),
                nameof(Static.DecimalProp76) => ResolveResult.Resolve(instance.DecimalProp76),
                nameof(Static.DecimalProp77) => ResolveResult.Resolve(instance.DecimalProp77),
                nameof(Static.DecimalProp78) => ResolveResult.Resolve(instance.DecimalProp78),
                nameof(Static.DecimalProp79) => ResolveResult.Resolve(instance.DecimalProp79),
                nameof(Static.DecimalProp80) => ResolveResult.Resolve(instance.DecimalProp80),
                nameof(Static.DecimalProp81) => ResolveResult.Resolve(instance.DecimalProp81),
                nameof(Static.DecimalProp82) => ResolveResult.Resolve(instance.DecimalProp82),
                nameof(Static.DecimalProp83) => ResolveResult.Resolve(instance.DecimalProp83),
                nameof(Static.DecimalProp84) => ResolveResult.Resolve(instance.DecimalProp84),
                nameof(Static.DecimalProp85) => ResolveResult.Resolve(instance.DecimalProp85),
                nameof(Static.DecimalProp86) => ResolveResult.Resolve(instance.DecimalProp86),
                nameof(Static.DecimalProp87) => ResolveResult.Resolve(instance.DecimalProp87),
                nameof(Static.DecimalProp88) => ResolveResult.Resolve(instance.DecimalProp88),
                nameof(Static.DecimalProp89) => ResolveResult.Resolve(instance.DecimalProp89),
                nameof(Static.DecimalProp90) => ResolveResult.Resolve(instance.DecimalProp90),
                nameof(Static.DecimalProp91) => ResolveResult.Resolve(instance.DecimalProp91),
                nameof(Static.DecimalProp92) => ResolveResult.Resolve(instance.DecimalProp92),
                nameof(Static.DecimalProp93) => ResolveResult.Resolve(instance.DecimalProp93),
                nameof(Static.DecimalProp94) => ResolveResult.Resolve(instance.DecimalProp94),
                nameof(Static.DecimalProp95) => ResolveResult.Resolve(instance.DecimalProp95),
                nameof(Static.DecimalProp96) => ResolveResult.Resolve(instance.DecimalProp96),
                nameof(Static.DecimalProp97) => ResolveResult.Resolve(instance.DecimalProp97),
                nameof(Static.DecimalProp98) => ResolveResult.Resolve(instance.DecimalProp98),
                nameof(Static.DecimalProp99) => ResolveResult.Resolve(instance.DecimalProp99),
                nameof(Static.StringProp1) => ResolveResult.Resolve(instance.StringProp1),
                nameof(Static.StringProp2) => ResolveResult.Resolve(instance.StringProp2),
                nameof(Static.StringProp3) => ResolveResult.Resolve(instance.StringProp3),
                nameof(Static.StringProp4) => ResolveResult.Resolve(instance.StringProp4),
                nameof(Static.StringProp5) => ResolveResult.Resolve(instance.StringProp5),
                nameof(Static.StringProp6) => ResolveResult.Resolve(instance.StringProp6),
                nameof(Static.StringProp7) => ResolveResult.Resolve(instance.StringProp7),
                nameof(Static.StringProp8) => ResolveResult.Resolve(instance.StringProp8),
                nameof(Static.StringProp9) => ResolveResult.Resolve(instance.StringProp9),
                nameof(Static.StringProp10) => ResolveResult.Resolve(instance.StringProp10),
                nameof(Static.StringProp11) => ResolveResult.Resolve(instance.StringProp11),
                nameof(Static.StringProp12) => ResolveResult.Resolve(instance.StringProp12),
                nameof(Static.StringProp13) => ResolveResult.Resolve(instance.StringProp13),
                nameof(Static.StringProp14) => ResolveResult.Resolve(instance.StringProp14),
                nameof(Static.StringProp15) => ResolveResult.Resolve(instance.StringProp15),
                nameof(Static.StringProp16) => ResolveResult.Resolve(instance.StringProp16),
                nameof(Static.StringProp17) => ResolveResult.Resolve(instance.StringProp17),
                nameof(Static.StringProp18) => ResolveResult.Resolve(instance.StringProp18),
                nameof(Static.StringProp19) => ResolveResult.Resolve(instance.StringProp19),
                nameof(Static.StringProp20) => ResolveResult.Resolve(instance.StringProp20),
                nameof(Static.StringProp21) => ResolveResult.Resolve(instance.StringProp21),
                nameof(Static.StringProp22) => ResolveResult.Resolve(instance.StringProp22),
                nameof(Static.StringProp23) => ResolveResult.Resolve(instance.StringProp23),
                nameof(Static.StringProp24) => ResolveResult.Resolve(instance.StringProp24),
                nameof(Static.StringProp25) => ResolveResult.Resolve(instance.StringProp25),
                nameof(Static.StringProp26) => ResolveResult.Resolve(instance.StringProp26),
                nameof(Static.StringProp27) => ResolveResult.Resolve(instance.StringProp27),
                nameof(Static.StringProp28) => ResolveResult.Resolve(instance.StringProp28),
                nameof(Static.StringProp29) => ResolveResult.Resolve(instance.StringProp29),
                nameof(Static.StringProp30) => ResolveResult.Resolve(instance.StringProp30),
                nameof(Static.StringProp31) => ResolveResult.Resolve(instance.StringProp31),
                nameof(Static.StringProp32) => ResolveResult.Resolve(instance.StringProp32),
                nameof(Static.StringProp33) => ResolveResult.Resolve(instance.StringProp33),
                nameof(Static.StringProp34) => ResolveResult.Resolve(instance.StringProp34),
                nameof(Static.StringProp35) => ResolveResult.Resolve(instance.StringProp35),
                nameof(Static.StringProp36) => ResolveResult.Resolve(instance.StringProp36),
                nameof(Static.StringProp37) => ResolveResult.Resolve(instance.StringProp37),
                nameof(Static.StringProp38) => ResolveResult.Resolve(instance.StringProp38),
                nameof(Static.StringProp39) => ResolveResult.Resolve(instance.StringProp39),
                nameof(Static.StringProp40) => ResolveResult.Resolve(instance.StringProp40),
                nameof(Static.StringProp41) => ResolveResult.Resolve(instance.StringProp41),
                nameof(Static.StringProp42) => ResolveResult.Resolve(instance.StringProp42),
                nameof(Static.StringProp43) => ResolveResult.Resolve(instance.StringProp43),
                nameof(Static.StringProp44) => ResolveResult.Resolve(instance.StringProp44),
                nameof(Static.StringProp45) => ResolveResult.Resolve(instance.StringProp45),
                nameof(Static.StringProp46) => ResolveResult.Resolve(instance.StringProp46),
                nameof(Static.StringProp47) => ResolveResult.Resolve(instance.StringProp47),
                nameof(Static.StringProp48) => ResolveResult.Resolve(instance.StringProp48),
                nameof(Static.StringProp49) => ResolveResult.Resolve(instance.StringProp49),
                nameof(Static.StringProp50) => ResolveResult.Resolve(instance.StringProp50),
                nameof(Static.StringProp51) => ResolveResult.Resolve(instance.StringProp51),
                nameof(Static.StringProp52) => ResolveResult.Resolve(instance.StringProp52),
                nameof(Static.StringProp53) => ResolveResult.Resolve(instance.StringProp53),
                nameof(Static.StringProp54) => ResolveResult.Resolve(instance.StringProp54),
                nameof(Static.StringProp55) => ResolveResult.Resolve(instance.StringProp55),
                nameof(Static.StringProp56) => ResolveResult.Resolve(instance.StringProp56),
                nameof(Static.StringProp57) => ResolveResult.Resolve(instance.StringProp57),
                nameof(Static.StringProp58) => ResolveResult.Resolve(instance.StringProp58),
                nameof(Static.StringProp59) => ResolveResult.Resolve(instance.StringProp59),
                nameof(Static.StringProp60) => ResolveResult.Resolve(instance.StringProp60),
                nameof(Static.StringProp61) => ResolveResult.Resolve(instance.StringProp61),
                nameof(Static.StringProp62) => ResolveResult.Resolve(instance.StringProp62),
                nameof(Static.StringProp63) => ResolveResult.Resolve(instance.StringProp63),
                nameof(Static.StringProp64) => ResolveResult.Resolve(instance.StringProp64),
                nameof(Static.StringProp65) => ResolveResult.Resolve(instance.StringProp65),
                nameof(Static.StringProp66) => ResolveResult.Resolve(instance.StringProp66),
                nameof(Static.StringProp67) => ResolveResult.Resolve(instance.StringProp67),
                nameof(Static.StringProp68) => ResolveResult.Resolve(instance.StringProp68),
                nameof(Static.StringProp69) => ResolveResult.Resolve(instance.StringProp69),
                nameof(Static.StringProp70) => ResolveResult.Resolve(instance.StringProp70),
                nameof(Static.StringProp71) => ResolveResult.Resolve(instance.StringProp71),
                nameof(Static.StringProp72) => ResolveResult.Resolve(instance.StringProp72),
                nameof(Static.StringProp73) => ResolveResult.Resolve(instance.StringProp73),
                nameof(Static.StringProp74) => ResolveResult.Resolve(instance.StringProp74),
                nameof(Static.StringProp75) => ResolveResult.Resolve(instance.StringProp75),
                nameof(Static.StringProp76) => ResolveResult.Resolve(instance.StringProp76),
                nameof(Static.StringProp77) => ResolveResult.Resolve(instance.StringProp77),
                nameof(Static.StringProp78) => ResolveResult.Resolve(instance.StringProp78),
                nameof(Static.StringProp79) => ResolveResult.Resolve(instance.StringProp79),
                nameof(Static.StringProp80) => ResolveResult.Resolve(instance.StringProp80),
                nameof(Static.StringProp81) => ResolveResult.Resolve(instance.StringProp81),
                nameof(Static.StringProp82) => ResolveResult.Resolve(instance.StringProp82),
                nameof(Static.StringProp83) => ResolveResult.Resolve(instance.StringProp83),
                nameof(Static.StringProp84) => ResolveResult.Resolve(instance.StringProp84),
                nameof(Static.StringProp85) => ResolveResult.Resolve(instance.StringProp85),
                nameof(Static.StringProp86) => ResolveResult.Resolve(instance.StringProp86),
                nameof(Static.StringProp87) => ResolveResult.Resolve(instance.StringProp87),
                nameof(Static.StringProp88) => ResolveResult.Resolve(instance.StringProp88),
                nameof(Static.StringProp89) => ResolveResult.Resolve(instance.StringProp89),
                nameof(Static.StringProp90) => ResolveResult.Resolve(instance.StringProp90),
                nameof(Static.StringProp91) => ResolveResult.Resolve(instance.StringProp91),
                nameof(Static.StringProp92) => ResolveResult.Resolve(instance.StringProp92),
                nameof(Static.StringProp93) => ResolveResult.Resolve(instance.StringProp93),
                nameof(Static.StringProp94) => ResolveResult.Resolve(instance.StringProp94),
                nameof(Static.StringProp95) => ResolveResult.Resolve(instance.StringProp95),
                nameof(Static.StringProp96) => ResolveResult.Resolve(instance.StringProp96),
                nameof(Static.StringProp97) => ResolveResult.Resolve(instance.StringProp97),
                nameof(Static.StringProp98) => ResolveResult.Resolve(instance.StringProp98),
                nameof(Static.StringProp99) => ResolveResult.Resolve(instance.StringProp99),
                nameof(Static.Parent) => ResolveResult.Resolve(instance.Parent),
                nameof(Static.Children) => ResolveResult.Resolve(instance.Children),
            
                _ => ResolveResult.Unresolved
            };
        }
    }

    public class Static
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        
        public decimal DecimalProp1 { get; set; }
        public decimal DecimalProp2 { get; set; }
        public decimal DecimalProp3 { get; set; }
        public decimal DecimalProp4 { get; set; }
        public decimal DecimalProp5 { get; set; }
        public decimal DecimalProp6 { get; set; }
        public decimal DecimalProp7 { get; set; }
        public decimal DecimalProp8 { get; set; }
        public decimal DecimalProp9 { get; set; }
        public decimal DecimalProp10 { get; set; }
        public decimal DecimalProp11 { get; set; }
        public decimal DecimalProp12 { get; set; }
        public decimal DecimalProp13 { get; set; }
        public decimal DecimalProp14 { get; set; }
        public decimal DecimalProp15 { get; set; }
        public decimal DecimalProp16 { get; set; }
        public decimal DecimalProp17 { get; set; }
        public decimal DecimalProp18 { get; set; }
        public decimal DecimalProp19 { get; set; }
        public decimal DecimalProp20 { get; set; }
        public decimal DecimalProp21 { get; set; }
        public decimal DecimalProp22 { get; set; }
        public decimal DecimalProp23 { get; set; }
        public decimal DecimalProp24 { get; set; }
        public decimal DecimalProp25 { get; set; }
        public decimal DecimalProp26 { get; set; }
        public decimal DecimalProp27 { get; set; }
        public decimal DecimalProp28 { get; set; }
        public decimal DecimalProp29 { get; set; }
        public decimal DecimalProp30 { get; set; }
        public decimal DecimalProp31 { get; set; }
        public decimal DecimalProp32 { get; set; }
        public decimal DecimalProp33 { get; set; }
        public decimal DecimalProp34 { get; set; }
        public decimal DecimalProp35 { get; set; }
        public decimal DecimalProp36 { get; set; }
        public decimal DecimalProp37 { get; set; }
        public decimal DecimalProp38 { get; set; }
        public decimal DecimalProp39 { get; set; }
        public decimal DecimalProp40 { get; set; }
        public decimal DecimalProp41 { get; set; }
        public decimal DecimalProp42 { get; set; }
        public decimal DecimalProp43 { get; set; }
        public decimal DecimalProp44 { get; set; }
        public decimal DecimalProp45 { get; set; }
        public decimal DecimalProp46 { get; set; }
        public decimal DecimalProp47 { get; set; }
        public decimal DecimalProp48 { get; set; }
        public decimal DecimalProp49 { get; set; }
        public decimal DecimalProp50 { get; set; }
        public decimal DecimalProp51 { get; set; }
        public decimal DecimalProp52 { get; set; }
        public decimal DecimalProp53 { get; set; }
        public decimal DecimalProp54 { get; set; }
        public decimal DecimalProp55 { get; set; }
        public decimal DecimalProp56 { get; set; }
        public decimal DecimalProp57 { get; set; }
        public decimal DecimalProp58 { get; set; }
        public decimal DecimalProp59 { get; set; }
        public decimal DecimalProp60 { get; set; }
        public decimal DecimalProp61 { get; set; }
        public decimal DecimalProp62 { get; set; }
        public decimal DecimalProp63 { get; set; }
        public decimal DecimalProp64 { get; set; }
        public decimal DecimalProp65 { get; set; }
        public decimal DecimalProp66 { get; set; }
        public decimal DecimalProp67 { get; set; }
        public decimal DecimalProp68 { get; set; }
        public decimal DecimalProp69 { get; set; }
        public decimal DecimalProp70 { get; set; }
        public decimal DecimalProp71 { get; set; }
        public decimal DecimalProp72 { get; set; }
        public decimal DecimalProp73 { get; set; }
        public decimal DecimalProp74 { get; set; }
        public decimal DecimalProp75 { get; set; }
        public decimal DecimalProp76 { get; set; }
        public decimal DecimalProp77 { get; set; }
        public decimal DecimalProp78 { get; set; }
        public decimal DecimalProp79 { get; set; }
        public decimal DecimalProp80 { get; set; }
        public decimal DecimalProp81 { get; set; }
        public decimal DecimalProp82 { get; set; }
        public decimal DecimalProp83 { get; set; }
        public decimal DecimalProp84 { get; set; }
        public decimal DecimalProp85 { get; set; }
        public decimal DecimalProp86 { get; set; }
        public decimal DecimalProp87 { get; set; }
        public decimal DecimalProp88 { get; set; }
        public decimal DecimalProp89 { get; set; }
        public decimal DecimalProp90 { get; set; }
        public decimal DecimalProp91 { get; set; }
        public decimal DecimalProp92 { get; set; }
        public decimal DecimalProp93 { get; set; }
        public decimal DecimalProp94 { get; set; }
        public decimal DecimalProp95 { get; set; }
        public decimal DecimalProp96 { get; set; }
        public decimal DecimalProp97 { get; set; }
        public decimal DecimalProp98 { get; set; }
        public decimal DecimalProp99 { get; set; }
        public string StringProp1 { get; set; }
        public string StringProp2 { get; set; }
        public string StringProp3 { get; set; }
        public string StringProp4 { get; set; }
        public string StringProp5 { get; set; }
        public string StringProp6 { get; set; }
        public string StringProp7 { get; set; }
        public string StringProp8 { get; set; }
        public string StringProp9 { get; set; }
        public string StringProp10 { get; set; }
        public string StringProp11 { get; set; }
        public string StringProp12 { get; set; }
        public string StringProp13 { get; set; }
        public string StringProp14 { get; set; }
        public string StringProp15 { get; set; }
        public string StringProp16 { get; set; }
        public string StringProp17 { get; set; }
        public string StringProp18 { get; set; }
        public string StringProp19 { get; set; }
        public string StringProp20 { get; set; }
        public string StringProp21 { get; set; }
        public string StringProp22 { get; set; }
        public string StringProp23 { get; set; }
        public string StringProp24 { get; set; }
        public string StringProp25 { get; set; }
        public string StringProp26 { get; set; }
        public string StringProp27 { get; set; }
        public string StringProp28 { get; set; }
        public string StringProp29 { get; set; }
        public string StringProp30 { get; set; }
        public string StringProp31 { get; set; }
        public string StringProp32 { get; set; }
        public string StringProp33 { get; set; }
        public string StringProp34 { get; set; }
        public string StringProp35 { get; set; }
        public string StringProp36 { get; set; }
        public string StringProp37 { get; set; }
        public string StringProp38 { get; set; }
        public string StringProp39 { get; set; }
        public string StringProp40 { get; set; }
        public string StringProp41 { get; set; }
        public string StringProp42 { get; set; }
        public string StringProp43 { get; set; }
        public string StringProp44 { get; set; }
        public string StringProp45 { get; set; }
        public string StringProp46 { get; set; }
        public string StringProp47 { get; set; }
        public string StringProp48 { get; set; }
        public string StringProp49 { get; set; }
        public string StringProp50 { get; set; }
        public string StringProp51 { get; set; }
        public string StringProp52 { get; set; }
        public string StringProp53 { get; set; }
        public string StringProp54 { get; set; }
        public string StringProp55 { get; set; }
        public string StringProp56 { get; set; }
        public string StringProp57 { get; set; }
        public string StringProp58 { get; set; }
        public string StringProp59 { get; set; }
        public string StringProp60 { get; set; }
        public string StringProp61 { get; set; }
        public string StringProp62 { get; set; }
        public string StringProp63 { get; set; }
        public string StringProp64 { get; set; }
        public string StringProp65 { get; set; }
        public string StringProp66 { get; set; }
        public string StringProp67 { get; set; }
        public string StringProp68 { get; set; }
        public string StringProp69 { get; set; }
        public string StringProp70 { get; set; }
        public string StringProp71 { get; set; }
        public string StringProp72 { get; set; }
        public string StringProp73 { get; set; }
        public string StringProp74 { get; set; }
        public string StringProp75 { get; set; }
        public string StringProp76 { get; set; }
        public string StringProp77 { get; set; }
        public string StringProp78 { get; set; }
        public string StringProp79 { get; set; }
        public string StringProp80 { get; set; }
        public string StringProp81 { get; set; }
        public string StringProp82 { get; set; }
        public string StringProp83 { get; set; }
        public string StringProp84 { get; set; }
        public string StringProp85 { get; set; }
        public string StringProp86 { get; set; }
        public string StringProp87 { get; set; }
        public string StringProp88 { get; set; }
        public string StringProp89 { get; set; }
        public string StringProp90 { get; set; }
        public string StringProp91 { get; set; }
        public string StringProp92 { get; set; }
        public string StringProp93 { get; set; }
        public string StringProp94 { get; set; }
        public string StringProp95 { get; set; }
        public string StringProp96 { get; set; }
        public string StringProp97 { get; set; }
        public string StringProp98 { get; set; }
        public string StringProp99 { get; set; }

        [IgnoreDataMember]
        public Static Parent { get; set; }
        public List<Static> Children { get; set; }
    }
}