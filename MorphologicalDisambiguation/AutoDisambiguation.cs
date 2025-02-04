using System;
using System.Collections.Generic;
using DataStructure;
using MorphologicalAnalysis;

namespace MorphologicalDisambiguation
{
    public abstract class AutoDisambiguation
    {
        protected FsmMorphologicalAnalyzer morphologicalAnalyzer;

        private static bool IsAnyWordSecondPerson(int index, List<FsmParse> correctParses)
        {
            var count = 0;
            for (var i = index - 1; i >= 0; i--)
            {
                if (correctParses[i].ContainsTag(MorphologicalTag.A2SG) ||
                    correctParses[i].ContainsTag(MorphologicalTag.P2SG))
                {
                    count++;
                }
            }

            return count >= 1;
        }

        private static bool IsPossessivePlural(int index, List<FsmParse> correctParses)
        {
            for (var i = index - 1; i >= 0; i--)
            {
                if (correctParses[i].IsNoun())
                {
                    return correctParses[i].IsPlural();
                }
            }

            return false;
        }

        private static string NextWordPos(FsmParseList nextParseList)
        {
            var map = new CounterHashMap<string>();
            for (var i = 0; i < nextParseList.Size(); i++)
            {
                map.Put(nextParseList.GetFsmParse(i).GetPos());
            }

            return map.Max();
        }

        private static bool IsBeforeLastWord(int index, FsmParseList[] fsmParses)
        {
            return index + 2 == fsmParses.Length;
        }

        private static bool NextWordExists(int index, FsmParseList[] fsmParses)
        {
            return index + 1 < fsmParses.Length;
        }

        private static bool IsNextWordNoun(int index, FsmParseList[] fsmParses)
        {
            return index + 1 < fsmParses.Length && NextWordPos(fsmParses[index + 1]).Equals("NOUN");
        }

        private static bool IsNextWordNum(int index, FsmParseList[] fsmParses)
        {
            return index + 1 < fsmParses.Length && NextWordPos(fsmParses[index + 1]).Equals("NUM");
        }

        private static bool IsNextWordNounOrAdjective(int index, FsmParseList[] fsmParses)
        {
            return index + 1 < fsmParses.Length && (NextWordPos(fsmParses[index + 1]).Equals("NOUN") ||
                                                    NextWordPos(fsmParses[index + 1]).Equals("ADJ") ||
                                                    NextWordPos(fsmParses[index + 1]).Equals("DET"));
        }

        private static bool IsFirstWord(int index)
        {
            return index == 0;
        }

        private static bool ContainsTwoNeOrYa(FsmParseList[] fsmParses, string word)
        {
            var count = 0;
            foreach (var fsmPars in fsmParses)
            {
                var surfaceForm = fsmPars.GetFsmParse(0).GetSurfaceForm();
                if (surfaceForm.Equals(word, StringComparison.InvariantCultureIgnoreCase))
                {
                    count++;
                }
            }

            return count == 2;
        }

        private static bool HasPreviousWordTag(int index, List<FsmParse> correctParses, MorphologicalTag tag)
        {
            return index > 0 && correctParses[index - 1].ContainsTag(tag);
        }

        private static string SelectCaseForParseString(string parseString, int index, FsmParseList[] fsmParses,
            List<FsmParse> correctParses)
        {
            var surfaceForm = fsmParses[index].GetFsmParse(0).GetSurfaceForm();
            var root = fsmParses[index].GetFsmParse(0).GetWord().GetName();
            var lastWord = fsmParses[fsmParses.Length - 1].GetFsmParse(0).GetSurfaceForm();
            switch (parseString)
            {
                /* kısmını, duracağını, grubunun */
                case "P2SG$P3SG":
                    if (IsAnyWordSecondPerson(index, correctParses))
                    {
                        return "P2SG";
                    }

                    return "P3SG";
                case "A2SG+P2SG$A3SG+P3SG":
                    if (IsAnyWordSecondPerson(index, correctParses))
                    {
                        return "A2SG+P2SG";
                    }

                    return "A3SG+P3SG";
                /* BİR */
                case "ADJ$ADV$DET$NUM+CARD":
                    return "DET";
                /* tahminleri, işleri, hisseleri */
                case "A3PL+P3PL+NOM$A3PL+P3SG+NOM$A3PL+PNON+ACC$A3SG+P3PL+NOM":
                    if (IsPossessivePlural(index, correctParses))
                    {
                        return "A3SG+P3PL+NOM";
                    }

                    return "A3PL+P3SG+NOM";
                /* Ocak, Cuma, ABD */
                case "A3SG$PROP+A3SG":
                    if (index > 0)
                    {
                        return "PROP+A3SG";
                    }

                    break;
                /* şirketin, seçimlerin, borsacıların, kitapların */
                case "P2SG+NOM$PNON+GEN":
                    if (IsAnyWordSecondPerson(index, correctParses))
                    {
                        return "P2SG+NOM";
                    }

                    return "PNON+GEN";
                /* ÇOK */
                case "ADJ$ADV$DET$POSTP+PCABL":
                /* FAZLA */
                case "ADJ$ADV$POSTP+PCABL":
                    if (HasPreviousWordTag(index, correctParses, MorphologicalTag.ABLATIVE))
                    {
                        return "POSTP+PCABL";
                    }

                    if (index + 1 < fsmParses.Length)
                    {
                        switch (NextWordPos(fsmParses[index + 1]))
                        {
                            case "NOUN":
                                return "ADJ";
                            case "ADJ":
                            case "ADV":
                            case "VERB":
                                return "ADV";
                            default:
                                break;
                        }
                    }

                    break;
                case "ADJ$NOUN+A3SG+PNON+NOM":
                    if (IsNextWordNounOrAdjective(index, fsmParses))
                    {
                        return "ADJ";
                    }

                    return "NOUN+A3SG+PNON+NOM";
                /* fanatiklerini, senetlerini, olduklarını */
                case "A3PL+P2SG$A3PL+P3PL$A3PL+P3SG$A3SG+P3PL":
                    if (IsAnyWordSecondPerson(index, correctParses))
                    {
                        return "A3PL+P2SG";
                    }

                    if (IsPossessivePlural(index, correctParses))
                    {
                        return "A3SG+P3PL";
                    }
                    else
                    {
                        return "A3PL+P3SG";
                    }
                case "ADJ$NOUN+PROP+A3SG+PNON+NOM":
                    if (index > 0)
                    {
                        return "NOUN+PROP+A3SG+PNON+NOM";
                    }

                    break;
                /* BU, ŞU */
                case "DET$PRON+DEMONSP+A3SG+PNON+NOM":
                    if (IsNextWordNoun(index, fsmParses))
                    {
                        return "DET";
                    }

                    return "PRON+DEMONSP+A3SG+PNON+NOM";
                /* gelebilir */
                case "AOR^DB+ADJ+ZERO$AOR+A3SG":
                    if (IsBeforeLastWord(index, fsmParses))
                    {
                        return "AOR+A3SG";
                    }
                    else if (IsFirstWord(index))
                    {
                        return "AOR^DB+ADJ+ZERO";
                    }
                    else
                    {
                        if (IsNextWordNounOrAdjective(index, fsmParses))
                        {
                            return "AOR^DB+ADJ+ZERO";
                        }

                        return "AOR+A3SG";
                    }
                case "ADV$NOUN+A3SG+PNON+NOM":
                    return "ADV";
                case "ADJ$ADV":
                    if (IsNextWordNoun(index, fsmParses))
                    {
                        return "ADJ";
                    }

                    return "ADV";
                case "P2SG$PNON":
                    if (IsAnyWordSecondPerson(index, correctParses))
                    {
                        return "P2SG";
                    }

                    return "PNON";
                /* etti, kırdı */
                case "NOUN+A3SG+PNON+NOM^DB+VERB+ZERO$VERB+POS":
                    if (IsBeforeLastWord(index, fsmParses))
                    {
                        return "VERB+POS";
                    }

                    break;
                /* İLE */
                case "CONJ$POSTP+PCNOM":
                    return "POSTP+PCNOM";
                /* gelecek */
                case "POS^DB+ADJ+FUTPART+PNON$POS+FUT+A3SG":
                    if (IsBeforeLastWord(index, fsmParses))
                    {
                        return "POS+FUT+A3SG";
                    }

                    return "POS^DB+ADJ+FUTPART+PNON";
                case "ADJ^DB$NOUN+A3SG+PNON+NOM^DB":
                    if (root.Equals("yok") || root.Equals("düşük") || root.Equals("eksik") || root.Equals("rahat") ||
                        root.Equals("orta") || root.Equals("vasat"))
                    {
                        return "ADJ^DB";
                    }

                    return "NOUN+A3SG+PNON+NOM^DB";
                /* yaptık, şüphelendik */
                case "POS^DB+ADJ+PASTPART+PNON$POS^DB+NOUN+PASTPART+A3SG+PNON+NOM$POS+PAST+A1PL":
                    return "POS+PAST+A1PL";
                /* ederim, yaparım */
                case "AOR^DB+ADJ+ZERO^DB+NOUN+ZERO+A3SG+P1SG+NOM$AOR+A1SG":
                    return "AOR+A1SG";
                /* geçti, vardı, aldı */
                case "ADJ^DB+VERB+ZERO$VERB+POS":
                    if (root.Equals("var") && !IsPossessivePlural(index, correctParses))
                    {
                        return "ADJ^DB+VERB+ZERO";
                    }

                    return "VERB+POS";
                /* ancak */
                case "ADV$CONJ":
                    return "CONJ";
                /* yaptığı, ettiği */
                case "ADJ+PASTPART+P3SG$NOUN+PASTPART+A3SG+P3SG+NOM":
                    if (IsNextWordNounOrAdjective(index, fsmParses))
                    {
                        return "ADJ+PASTPART+P3SG";
                    }

                    return "NOUN+PASTPART+A3SG+P3SG+NOM";
                /* ÖNCE, SONRA */
                case "ADV$NOUN+A3SG+PNON+NOM$POSTP+PCABL":
                    if (HasPreviousWordTag(index, correctParses, MorphologicalTag.ABLATIVE))
                    {
                        return "POSTP+PCABL";
                    }

                    return "ADV";
                case "NARR^DB+ADJ+ZERO$NARR+A3SG":
                    if (IsBeforeLastWord(index, fsmParses))
                    {
                        return "NARR+A3SG";
                    }

                    return "NARR^DB+ADJ+ZERO";
                case "ADJ$NOUN+A3SG+PNON+NOM$NOUN+PROP+A3SG+PNON+NOM":
                    if (index > 0)
                    {
                        return "NOUN+PROP+A3SG+PNON+NOM";
                    }
                    else
                    {
                        if (IsNextWordNounOrAdjective(index, fsmParses))
                        {
                            return "ADJ";
                        }

                        return "NOUN+A3SG+PNON+NOM";
                    }
                /* ödediğim */
                case "ADJ+PASTPART+P1SG$NOUN+PASTPART+A3SG+P1SG+NOM":
                    if (IsNextWordNounOrAdjective(index, fsmParses))
                    {
                        return "ADJ+PASTPART+P1SG";
                    }

                    return "NOUN+PASTPART+A3SG+P1SG+NOM";
                /* O */
                case "DET$PRON+DEMONSP+A3SG+PNON+NOM$PRON+PERS+A3SG+PNON+NOM":
                    if (IsNextWordNoun(index, fsmParses))
                    {
                        return "DET";
                    }

                    return "PRON+PERS+A3SG+PNON+NOM";
                /* BAZI */
                case "ADJ$DET$PRON+QUANTP+A3SG+P3SG+NOM":
                    return "DET";
                /* ONUN, ONA, ONDAN, ONUNLA, OYDU, ONUNKİ */
                case "DEMONSP$PERS":
                    return "PERS";
                case "ADJ$NOUN+A3SG+PNON+NOM$VERB+POS+IMP+A2SG":
                    if (IsNextWordNounOrAdjective(index, fsmParses))
                    {
                        return "ADJ";
                    }

                    return "NOUN+A3SG+PNON+NOM";
                /* hazineler, kıymetler */
                case "A3PL+PNON+NOM$A3SG+PNON+NOM^DB+VERB+ZERO+PRES+A3PL$PROP+A3PL+PNON+NOM":
                    if (index > 0)
                    {
                        if (fsmParses[index].GetFsmParse(0).IsCapitalWord())
                        {
                            return "PROP+A3PL+PNON+NOM";
                        }

                        return "A3PL+PNON+NOM";
                    }

                    break;
                /* ARTIK, GERİ */
                case "ADJ$ADV$NOUN+A3SG+PNON+NOM":
                    if (root.Equals("artık"))
                    {
                        return "ADV";
                    }
                    else if (IsNextWordNoun(index, fsmParses))
                    {
                        return "ADJ";
                    }

                    return "ADV";
                case "P1SG+NOM$PNON+NOM^DB+VERB+ZERO+PRES+A1SG":
                    if (IsBeforeLastWord(index, fsmParses) || root.Equals("değil"))
                    {
                        return "PNON+NOM^DB+VERB+ZERO+PRES+A1SG";
                    }

                    return "P1SG+NOM";
                /* görülmektedir */
                case "POS^DB+NOUN+INF+A3SG+PNON+LOC^DB+VERB+ZERO+PRES$POS+PROG2":
                    return "POS+PROG2";
                /* NE */
                case "ADJ$ADV$CONJ$PRON+QUESP+A3SG+PNON+NOM":
                    if (lastWord.Equals("?"))
                    {
                        return "PRON+QUESP+A3SG+PNON+NOM";
                    }

                    if (ContainsTwoNeOrYa(fsmParses, "ne"))
                    {
                        return "CONJ";
                    }

                    if (IsNextWordNoun(index, fsmParses))
                    {
                        return "ADJ";
                    }

                    return "ADV";
                /* TÜM */
                case "DET$NOUN+A3SG+PNON+NOM":
                    return "DET";
                /* AZ */
                case "ADJ$ADV$POSTP+PCABL$VERB+POS+IMP+A2SG":
                    if (HasPreviousWordTag(index, correctParses, MorphologicalTag.ABLATIVE))
                    {
                        return "POSTP+PCABL";
                    }

                    if (IsNextWordNounOrAdjective(index, fsmParses))
                    {
                        return "ADJ";
                    }

                    return "ADV";
                /* görülmedik */
                case "NEG^DB+ADJ+PASTPART+PNON$NEG^DB+NOUN+PASTPART+A3SG+PNON+NOM$NEG+PAST+A1PL":
                    if (surfaceForm.Equals("alışılmadık"))
                    {
                        return "NEG^DB+ADJ+PASTPART+PNON";
                    }

                    return "NEG+PAST+A1PL";
                case "DATE$NUM+FRACTION":
                    return "NUM+FRACTION";
                /* giriş, satış, öpüş, vuruş */
                case "POS^DB+NOUN+INF3+A3SG+PNON+NOM$RECIP+POS+IMP+A2SG":
                    return "POS^DB+NOUN+INF3+A3SG+PNON+NOM";
                /* başka, yukarı */
                case "ADJ$POSTP+PCABL":
                    if (HasPreviousWordTag(index, correctParses, MorphologicalTag.ABLATIVE))
                    {
                        return "POSTP+PCABL";
                    }

                    return "ADJ";
                /* KARŞI */
                case "ADJ$ADV$NOUN+A3SG+PNON+NOM$POSTP+PCDAT":
                    if (HasPreviousWordTag(index, correctParses, MorphologicalTag.DATIVE))
                    {
                        return "POSTP+PCDAT";
                    }

                    if (IsNextWordNoun(index, fsmParses))
                    {
                        return "ADJ";
                    }

                    return "ADV";
                /* BEN */
                case "NOUN+A3SG$NOUN+PROP+A3SG$PRON+PERS+A1SG":
                    return "PRON+PERS+A1SG";
                /* yapıcı, verici */
                case "ADJ+AGT$NOUN+AGT+A3SG+PNON+NOM":
                    if (IsNextWordNounOrAdjective(index, fsmParses))
                    {
                        return "ADJ+AGT";
                    }

                    return "NOUN+AGT+A3SG+PNON+NOM";
                /* BİLE */
                case "ADV$VERB+POS+IMP+A2SG":
                    return "ADV";
                /* ortalamalar, uzaylılar, demokratlar */
                case "NOUN+ZERO+A3PL+PNON+NOM$VERB+ZERO+PRES+A3PL":
                    return "NOUN+ZERO+A3PL+PNON+NOM";
                /* yasa, diye, yıla */
                case "NOUN+A3SG+PNON+DAT$VERB+POS+OPT+A3SG":
                    return "NOUN+A3SG+PNON+DAT";
                /* BİZ, BİZE */
                case "NOUN+A3SG$PRON+PERS+A1PL":
                    return "PRON+PERS+A1PL";
                /* AZDI */
                case "ADJ^DB+VERB+ZERO$POSTP+PCABL^DB+VERB+ZERO$VERB+POS":
                    return "ADJ^DB+VERB+ZERO";
                /* BİRİNCİ, İKİNCİ, ÜÇÜNCÜ, DÖRDÜNCÜ, BEŞİNCİ */
                case "ADJ$NUM+ORD":
                    return "ADJ";
                /* AY */
                case "INTERJ$NOUN+A3SG+PNON+NOM$VERB+POS+IMP+A2SG":
                    return "NOUN+A3SG+PNON+NOM";
                /* konuşmam, savunmam, etmem */
                case "NEG+AOR+A1SG$POS^DB+NOUN+INF2+A3SG+P1SG+NOM":
                    return "NEG+AOR+A1SG";
                /* YA */
                case "CONJ$INTERJ":
                    if (ContainsTwoNeOrYa(fsmParses, "ya"))
                    {
                        return "CONJ";
                    }

                    if (NextWordExists(index, fsmParses) &&
                        fsmParses[index + 1].GetFsmParse(0).GetSurfaceForm().Equals("da", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return "CONJ";
                    }

                    return "INTERJ";
                case "A3PL+P3PL$A3PL+P3SG$A3SG+P3PL":
                    if (IsPossessivePlural(index, correctParses))
                    {
                        return "A3SG+P3PL";
                    }

                    return "A3PL+P3SG";
                /* YÜZDE, YÜZLÜ */
                case "NOUN$NUM+CARD^DB+NOUN+ZERO":
                    return "NOUN";
                /* almanlar, uzmanlar, elmaslar, katiller */
                case "ADJ^DB+VERB+ZERO+PRES+A3PL$NOUN+A3PL+PNON+NOM$NOUN+A3SG+PNON+NOM^DB+VERB+ZERO+PRES+A3PL":
                    return "NOUN+A3PL+PNON+NOM";
                /* fazlası, yetkilisi */
                case "ADJ+JUSTLIKE$NOUN+ZERO+A3SG+P3SG+NOM":
                    return "NOUN+ZERO+A3SG+P3SG+NOM";
                /* HERKES, HERKESTEN, HERKESLE, HERKES */
                case "NOUN+A3SG+PNON$PRON+QUANTP+A3PL+P3PL":
                    return "PRON+QUANTP+A3PL+P3PL";
                /* BEN, BENDEN, BENCE, BANA, BENDE */
                case "NOUN+A3SG$PRON+PERS+A1SG":
                    return "PRON+PERS+A1SG";
                /* karşısından, geriye, geride */
                case "ADJ^DB+NOUN+ZERO$NOUN":
                    return "ADJ^DB+NOUN+ZERO";
                /* gideceği, kalacağı */
                case "ADJ+FUTPART+P3SG$NOUN+FUTPART+A3SG+P3SG+NOM":
                    if (IsNextWordNounOrAdjective(index, fsmParses))
                    {
                        return "ADJ+FUTPART+P3SG";
                    }

                    return "NOUN+FUTPART+A3SG+P3SG+NOM";
                /* bildiğimiz, geçtiğimiz, yaşadığımız */
                case "ADJ+PASTPART+P1PL$NOUN+PASTPART+A3SG+P1PL+NOM":
                    return "ADJ+PASTPART+P1PL";
                /* eminim, memnunum, açım */
                case "NOUN+ZERO+A3SG+P1SG+NOM$VERB+ZERO+PRES+A1SG":
                    return "VERB+ZERO+PRES+A1SG";
                /* yaparlar, olabilirler, değiştirirler */
                case "AOR^DB+ADJ+ZERO^DB+NOUN+ZERO+A3PL+PNON+NOM$AOR+A3PL":
                    return "AOR+A3PL";
                /* san, yasa */
                case "NOUN+A3SG+PNON+NOM$NOUN+PROP+A3SG+PNON+NOM$VERB+POS+IMP+A2SG":
                    if (index > 0)
                    {
                        return "NOUN+PROP+A3SG+PNON+NOM";
                    }

                    break;
                /* etmeyecek, yapmayacak, koşmayacak */
                case "NEG^DB+ADJ+FUTPART+PNON$NEG+FUT+A3SG":
                    return "NEG+FUT+A3SG";
                /* etmeli, olmalı */
                case "POS^DB+NOUN+INF2+A3SG+PNON+NOM^DB+ADJ+WITH$POS+NECES+A3SG":
                    if (IsBeforeLastWord(index, fsmParses))
                    {
                        return "POS+NECES+A3SG";
                    }

                    if (IsNextWordNounOrAdjective(index, fsmParses))
                    {
                        return "POS^DB+NOUN+INF2+A3SG+PNON+NOM^DB+ADJ+WITH";
                    }

                    return "POS+NECES+A3SG";
                /* DE */
                case "CONJ$NOUN+PROP+A3SG+PNON+NOM$VERB+POS+IMP+A2SG":
                    if (index > 0)
                    {
                        return "NOUN+PROP+A3SG+PNON+NOM";
                    }

                    break;
                /* GEÇ, SIK */
                case "ADJ$ADV$VERB+POS+IMP+A2SG":
                    if (surfaceForm.Equals("sık"))
                    {
                        var previousWord = "";
                        var nextWord = "";
                        if (index - 1 > -1)
                        {
                            previousWord = fsmParses[index - 1].GetFsmParse(0).GetSurfaceForm();
                        }

                        if (index + 1 < fsmParses.Length)
                        {
                            nextWord = fsmParses[index + 1].GetFsmParse(0).GetSurfaceForm();
                        }

                        if (previousWord.Equals("sık") || nextWord.Equals("sık"))
                        {
                            return "ADV";
                        }
                    }

                    if (IsNextWordNoun(index, fsmParses))
                    {
                        return "ADJ";
                    }

                    return "ADV";
                /* BİRLİKTE */
                case "ADV$POSTP+PCINS":
                    if (HasPreviousWordTag(index, correctParses, MorphologicalTag.INSTRUMENTAL))
                    {
                        return "POSTP+PCINS";
                    }

                    return "ADV";
                /* yavaşça, dürüstçe, fazlaca */
                case "ADJ+ASIF$ADV+LY$NOUN+ZERO+A3SG+PNON+EQU":
                    return "ADV+LY";
                /* FAZLADIR, FAZLAYDI, ÇOKTU, ÇOKTUR */
                case "ADJ^DB$POSTP+PCABL^DB":
                    if (HasPreviousWordTag(index, correctParses, MorphologicalTag.ABLATIVE))
                    {
                        return "POSTP+PCABL^DB";
                    }

                    return "ADJ^DB";
                /* kaybettikleri, umdukları, gösterdikleri */
                case
                    "ADJ+PASTPART+P3PL$NOUN+PASTPART+A3PL+P3PL+NOM$NOUN+PASTPART+A3PL+P3SG+NOM$NOUN+PASTPART+A3SG+P3PL+NOM"
                    :
                    if (IsNextWordNounOrAdjective(index, fsmParses))
                    {
                        return "ADJ+PASTPART+P3PL";
                    }

                    if (IsPossessivePlural(index, correctParses))
                    {
                        return "NOUN+PASTPART+A3SG+P3PL+NOM";
                    }

                    return "NOUN+PASTPART+A3PL+P3SG+NOM";
                /* yılın, yolun */
                case "NOUN+A3SG+P2SG+NOM$NOUN+A3SG+PNON+GEN$VERB^DB+VERB+PASS+POS+IMP+A2SG$VERB+POS+IMP+A2PL":
                    if (IsAnyWordSecondPerson(index, correctParses))
                    {
                        return "NOUN+A3SG+P2SG+NOM";
                    }

                    return "NOUN+A3SG+PNON+GEN";
                /* sürmekte, beklenmekte, değişmekte */
                case "POS^DB+NOUN+INF+A3SG+PNON+LOC$POS+PROG2+A3SG":
                    return "POS+PROG2+A3SG";
                /* KİMSE, KİMSEDE, KİMSEYE */
                case "NOUN+A3SG+PNON$PRON+QUANTP+A3SG+P3SG":
                    return "PRON+QUANTP+A3SG+P3SG";
                /* DOĞRU */
                case "ADJ$NOUN+A3SG+PNON+NOM$POSTP+PCDAT":
                    if (HasPreviousWordTag(index, correctParses, MorphologicalTag.DATIVE))
                    {
                        return "POSTP+PCDAT";
                    }

                    return "ADJ";
                /* ikisini, ikisine, fazlasına */
                case "ADJ+JUSTLIKE^DB+NOUN+ZERO+A3SG+P2SG$NOUN+ZERO+A3SG+P3SG":
                    return "NOUN+ZERO+A3SG+P3SG";
                /* kişilerdir, aylardır, yıllardır */
                case
                    "A3PL+PNON+NOM^DB+ADV+SINCE$A3PL+PNON+NOM^DB+VERB+ZERO+PRES+COP+A3SG$A3SG+PNON+NOM^DB+VERB+ZERO+PRES+A3PL+COP"
                    :
                    if (root.Equals("yıl", StringComparison.InvariantCultureIgnoreCase) || root.Equals("süre", StringComparison.InvariantCultureIgnoreCase) ||
                        root.Equals("zaman", StringComparison.InvariantCultureIgnoreCase) || root.Equals("ay", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return "A3PL+PNON+NOM^DB+ADV+SINCE";
                    }
                    else
                    {
                        return "A3PL+PNON+NOM^DB+VERB+ZERO+PRES+COP+A3SG";
                    }
                /* HEP */
                case "ADV$PRON+QUANTP+A3SG+P3SG+NOM":
                    return "ADV";
                /* O */
                case "DET$NOUN+PROP+A3SG+PNON+NOM$PRON+DEMONSP+A3SG+PNON+NOM$PRON+PERS+A3SG+PNON+NOM":
                    if (IsNextWordNoun(index, fsmParses))
                    {
                        return "DET";
                    }
                    else
                    {
                        return "PRON+PERS+A3SG+PNON+NOM";
                    }
                /* yapmalıyız, etmeliyiz, alınmalıdır */
                case "POS^DB+NOUN+INF2+A3SG+PNON+NOM^DB+ADJ+WITH^DB+VERB+ZERO+PRES$POS+NECES":
                    return "POS+NECES";
                /* kızdı, çekti, bozdu */
                case "ADJ^DB+VERB+ZERO$NOUN+A3SG+PNON+NOM^DB+VERB+ZERO$VERB+POS":
                    return "VERB+POS";
                /* BİZİMLE */
                case "NOUN+A3SG+P1SG$PRON+PERS+A1PL+PNON":
                    return "PRON+PERS+A1PL+PNON";
                /* VARDIR */
                case "ADJ^DB+VERB+ZERO+PRES+COP+A3SG$VERB^DB+VERB+CAUS+POS+IMP+A2SG":
                    return "ADJ^DB+VERB+ZERO+PRES+COP+A3SG";
                /* Mİ */
                case "NOUN+A3SG+PNON+NOM$QUES+PRES+A3SG":
                    return "QUES+PRES+A3SG";
                /* BENİM */
                case
                    "NOUN+A3SG+P1SG+NOM$NOUN+A3SG+PNON+NOM^DB+VERB+ZERO+PRES+A1SG$PRON+PERS+A1SG+PNON+GEN$PRON+PERS+A1SG+PNON+NOM^DB+VERB+ZERO+PRES+A1SG"
                    :
                    return "PRON+PERS+A1SG+PNON+GEN";
                /* SUN */
                case "NOUN+PROP+A3SG+PNON+NOM$VERB+POS+IMP+A2SG":
                    return "NOUN+PROP+A3SG+PNON+NOM";
                case "ADJ+JUSTLIKE$NOUN+ZERO^DB+ADJ+ALMOST$NOUN+ZERO+A3SG+P3SG+NOM":
                    return "NOUN+ZERO+A3SG+P3SG+NOM";
                /* düşündük, ettik, kazandık */
                case
                    "NOUN+A3SG+PNON+NOM^DB+VERB+ZERO+PAST+A1PL$VERB+POS^DB+ADJ+PASTPART+PNON$VERB+POS^DB+NOUN+PASTPART+A3SG+PNON+NOM$VERB+POS+PAST+A1PL"
                    :
                    return "VERB+POS+PAST+A1PL";
                /* komiktir, eksiktir, mevcuttur, yoktur */
                case
                    "ADJ^DB+VERB+ZERO+PRES+COP+A3SG$NOUN+A3SG+PNON+NOM^DB+ADV+SINCE$NOUN+A3SG+PNON+NOM^DB+VERB+ZERO+PRES+COP+A3SG"
                    :
                    return "ADJ^DB+VERB+ZERO+PRES+COP+A3SG";
                /* edeceğim, ekeceğim, koşacağım, gideceğim, savaşacağım, olacağım  */
                case "POS^DB+ADJ+FUTPART+P1SG$POS^DB+NOUN+FUTPART+A3SG+P1SG+NOM$POS+FUT+A1SG":
                    return "POS+FUT+A1SG";
                /* A */
                case "ADJ$INTERJ$NOUN+PROP+A3SG+PNON+NOM":
                    return "NOUN+PROP+A3SG+PNON+NOM";
                /* BİZİ */
                case "NOUN+A3SG+P3SG+NOM$NOUN+A3SG+PNON+ACC$PRON+PERS+A1PL+PNON+ACC":
                    return "PRON+PERS+A1PL+PNON+ACC";
                /* BİZİM */
                case
                    "NOUN+A3SG+P1SG+NOM$NOUN+A3SG+PNON+NOM^DB+VERB+ZERO+PRES+A1SG$PRON+PERS+A1PL+PNON+GEN$PRON+PERS+A1PL+PNON+NOM^DB+VERB+ZERO+PRES+A1SG"
                    :
                    return "PRON+PERS+A1PL+PNON+GEN";
                /* erkekler, kadınlar, madenler, uzmanlar*/
                case
                    "ADJ^DB+VERB+ZERO+PRES+A3PL$NOUN+A3PL+PNON+NOM$NOUN+A3SG+PNON+NOM^DB+VERB+ZERO+PRES+A3PL$NOUN+PROP+A3PL+PNON+NOM"
                    :
                    return "NOUN+A3PL+PNON+NOM";
                /* TABİ */
                case "ADJ$INTERJ":
                    return "ADJ";
                case "AOR^DB+ADJ+ZERO^DB+ADJ+JUSTLIKE^DB+NOUN+ZERO+A3SG+P2PL+NOM$AOR+A2PL":
                    return "AOR+A2PL";
                /* ayın, düşünün*/
                case "NOUN+A3SG+P2SG+NOM$NOUN+A3SG+PNON+GEN$VERB+POS+IMP+A2PL":
                    if (IsBeforeLastWord(index, fsmParses))
                    {
                        return "VERB+POS+IMP+A2PL";
                    }

                    return "NOUN+A3SG+PNON+GEN";
                /* ödeyecekler, olacaklar */
                case "POS^DB+NOUN+FUTPART+A3PL+PNON+NOM$POS+FUT+A3PL":
                    return "POS+FUT+A3PL";
                /* 9:30'daki */
                case "P3SG$PNON":
                    return "PNON";
                /* olabilecek, yapabilecek */
                case "ABLE^DB+ADJ+FUTPART+PNON$ABLE+FUT+A3SG":
                    if (IsNextWordNounOrAdjective(index, fsmParses))
                    {
                        return "ABLE^DB+ADJ+FUTPART+PNON";
                    }

                    return "ABLE+FUT+A3SG";
                /* düşmüş duymuş artmış */
                case "NOUN+A3SG+PNON+NOM^DB+VERB+ZERO+NARR+A3SG$VERB+POS+NARR^DB+ADJ+ZERO$VERB+POS+NARR+A3SG":
                    if (IsBeforeLastWord(index, fsmParses))
                    {
                        return "VERB+POS+NARR+A3SG";
                    }

                    return "VERB+POS+NARR^DB+ADJ+ZERO";
                /* BERİ, DIŞARI, AŞAĞI */
                case "ADJ$ADV$NOUN+A3SG+PNON+NOM$POSTP+PCABL":
                    if (HasPreviousWordTag(index, correctParses, MorphologicalTag.ABLATIVE))
                    {
                        return "POSTP+PCABL";
                    }

                    return "ADV";
                /* TV, CD */
                case "A3SG+PNON+ACC$PROP+A3SG+PNON+NOM":
                    return "A3SG+PNON+ACC";
                /* değinmeyeceğim, vermeyeceğim */
                case "NEG^DB+ADJ+FUTPART+P1SG$NEG^DB+NOUN+FUTPART+A3SG+P1SG+NOM$NEG+FUT+A1SG":
                    return "NEG+FUT+A1SG";
                /* görünüşe, satışa, duruşa */
                case "POS^DB+NOUN+INF3+A3SG+PNON+DAT$RECIP+POS+OPT+A3SG":
                    return "POS^DB+NOUN+INF3+A3SG+PNON+DAT";
                /* YILDIR, AYDIR, YOLDUR */
                case
                    "NOUN+A3SG+PNON+NOM^DB+ADV+SINCE$NOUN+A3SG+PNON+NOM^DB+VERB+ZERO+PRES+COP+A3SG$VERB^DB+VERB+CAUS+POS+IMP+A2SG"
                    :
                    if (root.Equals("yıl", StringComparison.InvariantCultureIgnoreCase) || root.Equals("ay", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return "NOUN+A3SG+PNON+NOM^DB+ADV+SINCE";
                    }
                    else
                    {
                        return "NOUN+A3SG+PNON+NOM^DB+VERB+ZERO+PRES+COP+A3SG";
                    }
                /* BENİ */
                case "NOUN+A3SG+P3SG+NOM$NOUN+A3SG+PNON+ACC$PRON+PERS+A1SG+PNON+ACC":
                    return "PRON+PERS+A1SG+PNON+ACC";
                /* edemezsin, kanıtlarsın, yapamazsın */
                case "AOR^DB+ADJ+ZERO^DB+ADJ+JUSTLIKE^DB+NOUN+ZERO+A3SG+P2SG+NOM$AOR+A2SG":
                    return "AOR+A2SG";
                /* BÜYÜME, ATAMA, KARIMA, KORUMA, TANIMA, ÜREME */
                case "NOUN+A3SG+P1SG+DAT$VERB+NEG+IMP+A2SG$VERB+POS^DB+NOUN+INF2+A3SG+PNON+NOM":
                    if (root.Equals("karı", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return "NOUN+A3SG+P1SG+DAT";
                    }

                    return "VERB+POS^DB+NOUN+INF2+A3SG+PNON+NOM";
                /* HANGİ */
                case "ADJ$PRON+QUESP+A3SG+PNON+NOM":
                    if (lastWord.Equals("?"))
                    {
                        return "PRON+QUESP+A3SG+PNON+NOM";
                    }

                    return "ADJ";
                /* GÜCÜNÜ, GÜCÜNÜN, ESASINDA */
                case "ADJ^DB+NOUN+ZERO+A3SG+P2SG$ADJ^DB+NOUN+ZERO+A3SG+P3SG$NOUN+A3SG+P2SG$NOUN+A3SG+P3SG":
                    return "NOUN+A3SG+P3SG";
                /* YILININ, YOLUNUN, DİLİNİN */
                case "NOUN+A3SG+P2SG+GEN$NOUN+A3SG+P3SG+GEN$VERB^DB+VERB+PASS+POS+IMP+A2PL":
                    return "NOUN+A3SG+P3SG+GEN";
                /* ÇIKARDI */
                case "VERB^DB+VERB+CAUS+POS$VERB+POS+AOR":
                    return "VERB+POS+AOR";
                /* sunucularımız, rakiplerimiz, yayınlarımız */
                case "P1PL+NOM$P1SG+NOM^DB+VERB+ZERO+PRES+A1PL":
                    return "P1PL+NOM";
                /* etmiştir, artmıştır, düşünmüştür, alınmıştır */
                case "NOUN+A3SG+PNON+NOM^DB+VERB+ZERO+NARR+A3SG+COP$VERB+POS+NARR+COP+A3SG":
                    return "VERB+POS+NARR+COP+A3SG";
                /* hazırlandı, yuvarlandı, temizlendi */
                case "VERB^DB+VERB+PASS$VERB+REFLEX":
                    return "VERB^DB+VERB+PASS";
                /* KARA, ÇEK, SOL, KOCA */
                case "ADJ$NOUN+A3SG+PNON+NOM$NOUN+PROP+A3SG+PNON+NOM$VERB+POS+IMP+A2SG":
                    if (index > 0)
                    {
                        if (fsmParses[index].GetFsmParse(0).IsCapitalWord())
                        {
                            return "NOUN+PROP+A3SG+PNON+NOM";
                        }

                        return "ADJ";
                    }

                    break;
                /* YÜZ */
                case "NOUN+A3SG+PNON+NOM$NUM+CARD$VERB+POS+IMP+A2SG":
                    if (IsNextWordNum(index, fsmParses))
                    {
                        return "NUM+CARD";
                    }

                    return "NOUN+A3SG+PNON+NOM";
                case "ADJ+AGT^DB+ADJ+JUSTLIKE$NOUN+AGT^DB+ADJ+ALMOST$NOUN+AGT+A3SG+P3SG+NOM":
                    return "NOUN+AGT+A3SG+P3SG+NOM";
                /* artışın, düşüşün, yükselişin*/
                case "POS^DB+NOUN+INF3+A3SG+P2SG+NOM$POS^DB+NOUN+INF3+A3SG+PNON+GEN$RECIP+POS+IMP+A2PL":
                    if (IsAnyWordSecondPerson(index, correctParses))
                    {
                        return "POS^DB+NOUN+INF3+A3SG+P2SG+NOM";
                    }

                    return "POS^DB+NOUN+INF3+A3SG+PNON+GEN";
                /* VARSA */
                case "ADJ^DB+VERB+ZERO+COND$VERB+POS+DESR":
                    return "ADJ^DB+VERB+ZERO+COND";
                /* DEK */
                case "NOUN+A3SG+PNON+NOM$POSTP+PCDAT":
                    return "POSTP+PCDAT";
                /* ALDIK */
                case
                    "ADJ^DB+VERB+ZERO+PAST+A1PL$VERB+POS^DB+ADJ+PASTPART+PNON$VERB+POS^DB+NOUN+PASTPART+A3SG+PNON+NOM$VERB+POS+PAST+A1PL"
                    :
                    return "VERB+POS+PAST+A1PL";
                /* BİRİNİN, BİRİNE, BİRİNİ, BİRİNDEN */
                case
                    "ADJ^DB+NOUN+ZERO+A3SG+P2SG$ADJ^DB+NOUN+ZERO+A3SG+P3SG$NUM+CARD^DB+NOUN+ZERO+A3SG+P2SG$NUM+CARD^DB+NOUN+ZERO+A3SG+P3SG"
                    :
                    return "NUM+CARD^DB+NOUN+ZERO+A3SG+P3SG";
                /* ARTIK */
                case "ADJ$ADV$NOUN+A3SG+PNON+NOM$NOUN+PROP+A3SG+PNON+NOM":
                    return "ADV";
                /* BİRİ */
                case
                    "ADJ^DB+NOUN+ZERO+A3SG+P3SG+NOM$ADJ^DB+NOUN+ZERO+A3SG+PNON+ACC$NUM+CARD^DB+NOUN+ZERO+A3SG+P3SG+NOM$NUM+CARD^DB+NOUN+ZERO+A3SG+PNON+ACC"
                    :
                    return "NUM+CARD^DB+NOUN+ZERO+A3SG+P3SG+NOM";
                /* DOĞRU */
                case "ADJ$NOUN+A3SG+PNON+NOM$NOUN+PROP+A3SG+PNON+NOM$POSTP+PCDAT":
                    if (HasPreviousWordTag(index, correctParses, MorphologicalTag.DATIVE))
                    {
                        return "POSTP+PCDAT";
                    }

                    return "ADJ";
                /* demiryolları, havayolları, milletvekilleri */
                case "P3PL+NOM$P3SG+NOM$PNON+ACC":
                    if (IsPossessivePlural(index, correctParses))
                    {
                        return "P3PL+NOM";
                    }

                    return "P3SG+NOM";
                /* GEREK */
                case "CONJ$NOUN+A3SG+PNON+NOM$VERB+POS+IMP+A2SG":
                    if (ContainsTwoNeOrYa(fsmParses, "gerek"))
                    {
                        return "CONJ";
                    }

                    return "NOUN+A3SG+PNON+NOM";
                /* bilmediğiniz, sevdiğiniz, kazandığınız */
                case "ADJ+PASTPART+P2PL$NOUN+PASTPART+A3SG+P2PL+NOM$NOUN+PASTPART+A3SG+PNON+GEN^DB+VERB+ZERO+PRES+A1PL":
                    if (IsNextWordNounOrAdjective(index, fsmParses))
                    {
                        return "ADJ+PASTPART+P2PL";
                    }

                    return "NOUN+PASTPART+A3SG+P2PL+NOM";
                /* yapabilecekleri, edebilecekleri, sunabilecekleri */
                case
                    "ADJ+FUTPART+P3PL$NOUN+FUTPART+A3PL+P3PL+NOM$NOUN+FUTPART+A3PL+P3SG+NOM$NOUN+FUTPART+A3PL+PNON+ACC$NOUN+FUTPART+A3SG+P3PL+NOM"
                    :
                    if (IsNextWordNounOrAdjective(index, fsmParses))
                    {
                        return "ADJ+FUTPART+P3PL";
                    }

                    if (IsPossessivePlural(index, correctParses))
                    {
                        return "NOUN+FUTPART+A3SG+P3PL+NOM";
                    }

                    return "NOUN+FUTPART+A3PL+P3SG+NOM";
                /* KİM */
                case "NOUN+PROP$PRON+QUESP":
                    if (lastWord.Equals("?"))
                    {
                        return "PRON+QUESP";
                    }

                    return "NOUN+PROP";
                /* ALINDI */
                case
                    "ADJ^DB+NOUN+ZERO+A3SG+P2SG+NOM^DB+VERB+ZERO$ADJ^DB+NOUN+ZERO+A3SG+PNON+GEN^DB+VERB+ZERO$VERB^DB+VERB+PASS+POS"
                    :
                    return "VERB^DB+VERB+PASS+POS";
                /* KIZIM */
                case "ADJ^DB+VERB+ZERO+PRES+A1SG$NOUN+A3SG+P1SG+NOM$NOUN+A3SG+PNON+NOM^DB+VERB+ZERO+PRES+A1SG":
                    return "NOUN+A3SG+P1SG+NOM";
                /* etmeliydi, yaratmalıydı */
                case "POS^DB+NOUN+INF2+A3SG+PNON+NOM^DB+ADJ+WITH^DB+VERB+ZERO$POS+NECES":
                    return "POS+NECES";
                /* HERKESİN */
                case "NOUN+A3SG+P2SG+NOM$NOUN+A3SG+PNON+GEN$PRON+QUANTP+A3PL+P3PL+GEN":
                    return "PRON+QUANTP+A3PL+P3PL+GEN";
                case "ADJ+JUSTLIKE^DB+NOUN+ZERO+A3SG+P2SG$ADJ+JUSTLIKE^DB+NOUN+ZERO+A3SG+PNON$NOUN+ZERO+A3SG+P3SG":
                    return "NOUN+ZERO+A3SG+P3SG";
                /* milyarlık, milyonluk, beşlik, ikilik */
                case "NESS+A3SG+PNON+NOM$ZERO+A3SG+PNON+NOM^DB+ADJ+FITFOR":
                    return "ZERO+A3SG+PNON+NOM^DB+ADJ+FITFOR";
                /* alınmamaktadır, koymamaktadır */
                case "NEG^DB+NOUN+INF+A3SG+PNON+LOC^DB+VERB+ZERO+PRES$NEG+PROG2":
                    return "NEG+PROG2";
                /* HEPİMİZ */
                case "A1PL+P1PL+NOM$A3SG+P3SG+GEN^DB+VERB+ZERO+PRES+A1PL":
                    return "A1PL+P1PL+NOM";
                /* KİMSENİN */
                case "NOUN+A3SG+P2SG$NOUN+A3SG+PNON$PRON+QUANTP+A3SG+P3SG":
                    return "PRON+QUANTP+A3SG+P3SG";
                /* GEÇMİŞ, ALMIŞ, VARMIŞ */
                case "ADJ^DB+VERB+ZERO+NARR+A3SG$VERB+POS+NARR^DB+ADJ+ZERO$VERB+POS+NARR+A3SG":
                    if (IsNextWordNounOrAdjective(index, fsmParses))
                    {
                        return "VERB+POS+NARR^DB+ADJ+ZERO";
                    }

                    return "VERB+POS+NARR+A3SG";
                /* yapacağınız, konuşabileceğiniz, olacağınız */
                case "ADJ+FUTPART+P2PL$NOUN+FUTPART+A3SG+P2PL+NOM$NOUN+FUTPART+A3SG+PNON+GEN^DB+VERB+ZERO+PRES+A1PL":
                    if (IsNextWordNounOrAdjective(index, fsmParses))
                    {
                        return "ADJ+FUTPART+P2PL";
                    }

                    return "NOUN+FUTPART+A3SG+P2PL+NOM";
                /* YILINA, DİLİNE, YOLUNA */
                case "NOUN+A3SG+P2SG+DAT$NOUN+A3SG+P3SG+DAT$VERB^DB+VERB+PASS+POS+OPT+A3SG":
                    if (IsAnyWordSecondPerson(index, correctParses))
                    {
                        return "NOUN+A3SG+P2SG+DAT";
                    }

                    return "NOUN+A3SG+P3SG+DAT";
                /* MİSİN, MİYDİ, MİSİNİZ */
                case "NOUN+A3SG+PNON+NOM^DB+VERB+ZERO$QUES":
                    return "QUES";
                /* ATAKLAR, GÜÇLER, ESASLAR */
                case
                    "ADJ^DB+NOUN+ZERO+A3PL+PNON+NOM$ADJ^DB+VERB+ZERO+PRES+A3PL$NOUN+A3PL+PNON+NOM$NOUN+A3SG+PNON+NOM^DB+VERB+ZERO+PRES+A3PL"
                    :
                    return "NOUN+A3PL+PNON+NOM";
                case "A3PL+P3SG$A3SG+P3PL$PROP+A3PL+P3PL":
                    return "PROP+A3PL+P3PL";
                /* pilotunuz, suçunuz, haberiniz */
                case "P2PL+NOM$PNON+GEN^DB+VERB+ZERO+PRES+A1PL":
                    return "P2PL+NOM";
                /* yıllarca, aylarca, düşmanca */
                case "ADJ+ASIF$ADV+LY":
                    if (IsNextWordNounOrAdjective(index, fsmParses))
                    {
                        return "ADJ+ASIF";
                    }

                    return "ADV+LY";
                /* gerçekçi, alıcı */
                case "ADJ^DB+NOUN+AGT+A3SG+PNON+NOM$NOUN+A3SG+PNON+NOM^DB+ADJ+AGT":
                    if (IsNextWordNounOrAdjective(index, fsmParses))
                    {
                        return "NOUN+A3SG+PNON+NOM^DB+ADJ+AGT";
                    }

                    return "ADJ^DB+NOUN+AGT+A3SG+PNON+NOM";
                /* havayollarına, gözyaşlarına */
                case "P2SG$P3PL$P3SG":
                    if (IsAnyWordSecondPerson(index, correctParses))
                    {
                        return "P2SG";
                    }

                    if (IsPossessivePlural(index, correctParses))
                    {
                        return "P3PL";
                    }

                    return "P3SG";
                /* olun, kurtulun, gelin */
                case "VERB^DB+VERB+PASS+POS+IMP+A2SG$VERB+POS+IMP+A2PL":
                    return "VERB+POS+IMP+A2PL";
                case "ADJ+JUSTLIKE^DB$NOUN+ZERO+A3SG+P3SG+NOM^DB":
                    return "NOUN+ZERO+A3SG+P3SG+NOM^DB";
                /* oluşmaktaydı, gerekemekteydi */
                case "POS^DB+NOUN+INF+A3SG+PNON+LOC^DB+VERB+ZERO$POS+PROG2":
                    return "POS+PROG2";
                /* BERABER */
                case "ADJ$ADV$POSTP+PCINS":
                    if (HasPreviousWordTag(index, correctParses, MorphologicalTag.INSTRUMENTAL))
                    {
                        return "POSTP+PCINS";
                    }

                    if (IsNextWordNounOrAdjective(index, fsmParses))
                    {
                        return "ADJ";
                    }

                    return "ADV";
                /* BİN, KIRK */
                case "NUM+CARD$VERB+POS+IMP+A2SG":
                    return "NUM+CARD";
                /* ÖTE */
                case "NOUN+A3SG+PNON+NOM$POSTP+PCABL":
                    if (HasPreviousWordTag(index, correctParses, MorphologicalTag.ABLATIVE))
                    {
                        return "POSTP+PCABL";
                    }

                    return "NOUN+A3SG+PNON+NOM";
                /* BENİMLE */
                case "NOUN+A3SG+P1SG$PRON+PERS+A1SG+PNON":
                    return "PRON+PERS+A1SG+PNON";
                /* Accusative and Ablative Cases*/
                case "ADV+WITHOUTHAVINGDONESO$NOUN+INF2+A3SG+PNON+ABL":
                    return "ADV+WITHOUTHAVINGDONESO";
                case
                    "ADJ^DB+NOUN+ZERO+A3SG+P3SG+NOM$ADJ^DB+NOUN+ZERO+A3SG+PNON+ACC$NOUN+A3SG+P3SG+NOM$NOUN+A3SG+PNON+ACC"
                    :
                    return "ADJ^DB+NOUN+ZERO+A3SG+P3SG+NOM";
                case "P3SG+NOM$PNON+ACC":
                    if (fsmParses[index].GetFsmParse(0).GetFinalPos().Equals("PROP"))
                    {
                        return "PNON+ACC";
                    }
                    else
                    {
                        return "P3SG+NOM";
                    }
                case "A3PL+PNON+NOM$A3SG+PNON+NOM^DB+VERB+ZERO+PRES+A3PL":
                    return "A3PL+PNON+NOM";
                case "ADV+SINCE$VERB+ZERO+PRES+COP+A3SG":
                    if (root.Equals("yıl", StringComparison.InvariantCultureIgnoreCase) || root.Equals("süre", StringComparison.InvariantCultureIgnoreCase) ||
                        root.Equals("zaman", StringComparison.InvariantCultureIgnoreCase) || root.Equals("ay", StringComparison.InvariantCultureIgnoreCase))
                    {
                        return "ADV+SINCE";
                    }
                    else
                    {
                        return "VERB+ZERO+PRES+COP+A3SG";
                    }
                case "CONJ$VERB+POS+IMP+A2SG":
                    return "CONJ";
                case "NEG+IMP+A2SG$POS^DB+NOUN+INF2+A3SG+PNON+NOM":
                    return "POS^DB+NOUN+INF2+A3SG+PNON+NOM";
                case "NEG+OPT+A3SG$POS^DB+NOUN+INF2+A3SG+PNON+DAT":
                    return "POS^DB+NOUN+INF2+A3SG+PNON+DAT";
                case "NOUN^DB+ADJ+ALMOST$NOUN+A3SG+P3SG+NOM":
                    return "NOUN+A3SG+P3SG+NOM";
                case "ADJ$VERB+POS+IMP+A2SG":
                    return "ADJ";
                case "NOUN+A3SG+PNON+NOM$VERB+POS+IMP+A2SG":
                    return "NOUN+A3SG+PNON+NOM";
                case "INF2^DB+ADJ+ALMOST$INF2+A3SG+P3SG+NOM":
                    return "INF2+A3SG+P3SG+NOM";
            }

            return null;
        }

        public static FsmParse CaseDisambiguator(int index, FsmParseList[] fsmParses, List<FsmParse> correctParses)
        {
            var fsmParseList = fsmParses[index];
            var defaultCase = SelectCaseForParseString(fsmParses[index].ParsesWithoutPrefixAndSuffix(), index,
                fsmParses, correctParses);
            if (defaultCase != null)
            {
                for (var i = 0; i < fsmParseList.Size(); i++)
                {
                    var fsmParse = fsmParseList.GetFsmParse(i);
                    if (fsmParse.TransitionList().Contains(defaultCase))
                    {
                        return fsmParse;
                    }
                }
            }

            return null;
        }
    }
}