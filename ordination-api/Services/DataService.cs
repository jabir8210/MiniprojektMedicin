using Microsoft.EntityFrameworkCore;
using System.Text.Json;

using shared.Model;
using static shared.Util;
using Data;

namespace Service;

public class DataService
{
    private OrdinationContext db { get; }

    public DataService(OrdinationContext db) {
        this.db = db;
    }

    /// <summary>
    /// Seeder noget nyt data i databasen, hvis det er nødvendigt.
    /// </summary>
    public void SeedData() {

        // Patients
        Patient[] patients = new Patient[5];
        patients[0] = db.Patienter.FirstOrDefault()!;

        if (patients[0] == null)
        {
            patients[0] = new Patient("121256-0512", "Jane Jensen", 63.4);
            patients[1] = new Patient("070985-1153", "Finn Madsen", 83.2);
            patients[2] = new Patient("050972-1233", "Hans Jørgensen", 89.4);
            patients[3] = new Patient("011064-1522", "Ulla Nielsen", 59.9);
            patients[4] = new Patient("123456-1234", "Ib Hansen", 87.7);

            db.Patienter.Add(patients[0]);
            db.Patienter.Add(patients[1]);
            db.Patienter.Add(patients[2]);
            db.Patienter.Add(patients[3]);
            db.Patienter.Add(patients[4]);
            db.SaveChanges();
        }

        Laegemiddel[] laegemiddler = new Laegemiddel[5];
        laegemiddler[0] = db.Laegemiddler.FirstOrDefault()!;
        if (laegemiddler[0] == null)
        {
            laegemiddler[0] = new Laegemiddel("Acetylsalicylsyre", 0.1, 0.15, 0.16, "Styk");
            laegemiddler[1] = new Laegemiddel("Paracetamol", 1, 1.5, 2, "Ml");
            laegemiddler[2] = new Laegemiddel("Fucidin", 0.025, 0.025, 0.025, "Styk");
            laegemiddler[3] = new Laegemiddel("Methotrexat", 0.01, 0.015, 0.02, "Styk");
            laegemiddler[4] = new Laegemiddel("Prednisolon", 0.1, 0.15, 0.2, "Styk");

            db.Laegemiddler.Add(laegemiddler[0]);
            db.Laegemiddler.Add(laegemiddler[1]);
            db.Laegemiddler.Add(laegemiddler[2]);
            db.Laegemiddler.Add(laegemiddler[3]);
            db.Laegemiddler.Add(laegemiddler[4]);

            db.SaveChanges();
        }

        Ordination[] ordinationer = new Ordination[6];
        ordinationer[0] = db.Ordinationer.FirstOrDefault()!;
        if (ordinationer[0] == null) {
            Laegemiddel[] lm = db.Laegemiddler.ToArray();
            Patient[] p = db.Patienter.ToArray();

            ordinationer[0] = new PN(new DateTime(2024, 11, 1), new DateTime(2024, 11, 12), 123, lm[1]);    
            ordinationer[1] = new PN(new DateTime(2024, 11, 2), new DateTime(2024, 11, 14), 3, lm[0]);    
            ordinationer[2] = new PN(new DateTime(2024, 11, 2), new DateTime(2024, 11, 25), 5, lm[2]);    
            ordinationer[3] = new PN(new DateTime(2024, 11, 1), new DateTime(2024, 11, 12), 123, lm[1]);
            ordinationer[4] = new DagligFast(new DateTime(2024, 1, 1), new DateTime(2024, 11, 12), lm[1], 2, 0, 1, 0);
            ordinationer[5] = new DagligSkæv(new DateTime(2024, 1, 2), new DateTime(2024, 11, 20), lm[2]);
            
            ((DagligSkæv) ordinationer[5]).doser = new Dosis[] { 
                new Dosis(CreateTimeOnly(12, 0, 0), 0.5),
                new Dosis(CreateTimeOnly(12, 40, 0), 1),
                new Dosis(CreateTimeOnly(16, 0, 0), 2.5),
                new Dosis(CreateTimeOnly(18, 45, 0), 3)        
            }.ToList();
            

            db.Ordinationer.Add(ordinationer[0]);
            db.Ordinationer.Add(ordinationer[1]);
            db.Ordinationer.Add(ordinationer[2]);
            db.Ordinationer.Add(ordinationer[3]);
            db.Ordinationer.Add(ordinationer[4]);
            db.Ordinationer.Add(ordinationer[5]);

            db.SaveChanges();

            p[0].ordinationer.Add(ordinationer[0]);
            p[0].ordinationer.Add(ordinationer[1]);
            p[2].ordinationer.Add(ordinationer[2]);
            p[3].ordinationer.Add(ordinationer[3]);
            p[1].ordinationer.Add(ordinationer[4]);
            p[1].ordinationer.Add(ordinationer[5]);

            db.SaveChanges();
        }
    }

    
    public List<PN> GetPNs() {
        return db.PNs.Include(o => o.laegemiddel).Include(o => o.dates).ToList();
    }

    public List<DagligFast> GetDagligFaste() {
        return db.DagligFaste
            .Include(o => o.laegemiddel)
            .Include(o => o.MorgenDosis)
            .Include(o => o.MiddagDosis)
            .Include(o => o.AftenDosis)            
            .Include(o => o.NatDosis)            
            .ToList();
    }

    public List<DagligSkæv> GetDagligSkæve() {
        return db.DagligSkæve
            .Include(o => o.laegemiddel)
            .Include(o => o.doser)
            .ToList();
    }

    public List<Patient> GetPatienter() {
        return db.Patienter.Include(p => p.ordinationer).ToList();
    }

    public List<Laegemiddel> GetLaegemidler() {
        return db.Laegemiddler.ToList();
    }

    public PN OpretPN(int patientId, int laegemiddelId, double antal, DateTime startDato, DateTime slutDato) {
        Patient patient = db.Patienter.Find(patientId);
        Laegemiddel laegemiddel = db.Laegemiddler.Find(laegemiddelId);

        if (patient == null || laegemiddel == null)
        {
            throw new Exception("Patient eller lægemiddel ikke fundet");
        }

        // Check if the dosis is negative
        if (antal < 0)
        {
            throw new ArgumentException("Dosis mængde må ikke være negativ");
        }

        // Check if the dosis is 0
        if (antal == 0)
        {
            throw new ArgumentException("Dosis mængde må ikke være 0");
        }

        // overskridelse af anbefalet dosis
        if (antal > GetAnbefaletDosisPerDøgn(patientId, laegemiddelId))
        {
            throw new ArgumentException("Dosis overstiger anbefalet dosis");
        }

        // indenfor gyldighedsperiode
        if (startDato > slutDato)
        {
            throw new ArgumentException("Udenfor gyldighedsperioden");
        }


        // Check for registrerede doser
        if (startDato == slutDato && antal == 0)
        {
            throw new ArgumentException("Ingen doser registreret for denne periode");
        }



        var pn = new PN(startDato, slutDato, antal, laegemiddel);

        patient.ordinationer.Add(pn);
        db.Ordinationer.Add(pn);
        db.SaveChanges();
        return pn;
    }

    public DagligFast OpretDagligFast(int patientId, int laegemiddelId, 
        double antalMorgen, double antalMiddag, double antalAften, double antalNat, 
        DateTime startDato, DateTime slutDato) {
        Patient patient = db.Patienter.Find(patientId);
        Laegemiddel laegemiddel = db.Laegemiddler.Find(laegemiddelId);

        if (patient == null || laegemiddel == null)
        {
            throw new Exception("Patient eller lægemiddel ikke fundet");
        }

        // Check if the dosis is negative
        if (antalMorgen < 0 || antalMiddag < 0 || antalAften < 0 || antalNat < 0)
        {
            throw new ArgumentException("Dosis mængde må ikke være negativ");
        }

        // Check if the accumulated dosis is 0
        if (antalMorgen == 0 && antalMiddag == 0 && antalAften == 0 && antalNat == 0)
        {
            throw new ArgumentException("Dosis mængde må ikke være 0");
        }


        // Beregn anbefalet døgndosis
        double anbefaletDoegnDosis = GetAnbefaletDosisPerDøgn(patientId, laegemiddelId);

        // Beregn samlet dosis
        double samletDosis = antalMorgen + antalMiddag + antalAften + antalNat;

        // Valider dosis
        if (samletDosis > anbefaletDoegnDosis)
        {
            throw new ArgumentException($"Samlet dosis ({samletDosis}) overstiger anbefalet døgndosis ({anbefaletDoegnDosis})");
        }


        var dagligFast = new DagligFast(startDato, slutDato, laegemiddel, antalMorgen, antalMiddag, antalAften, antalNat);

        patient.ordinationer.Add(dagligFast);
        db.Ordinationer.Add(dagligFast);
        db.SaveChanges();

        return dagligFast;
    }

    public DagligSkæv OpretDagligSkaev(int patientId, int laegemiddelId, Dosis[] doser, DateTime startDato, DateTime slutDato)
    {

        // Find patienten og lægemidlet
        var patient = db.Patienter.Include(p => p.ordinationer).FirstOrDefault(p => p.PatientId == patientId);
        var laegemiddel = db.Laegemiddler.FirstOrDefault(l => l.LaegemiddelId == laegemiddelId);

        if (patient == null || laegemiddel == null)
        {
            throw new ArgumentException("Patient eller lægemiddel ikke fundet.");
        }

        // Check if the dosis is negative
        foreach (var dosis in doser)
        {
            if (dosis.antal < 0)
            {
                throw new ArgumentException("Dosis mængde må ikke være negativ");
            }
        }

        // Check if the time chosen is valid
        foreach (var dosis in doser)
        {
            if (dosis.tid.Hour < 0 || dosis.tid.Hour > 23 || dosis.tid.Minute < 0 || dosis.tid.Minute > 59 || dosis.tid.Second < 0 || dosis.tid.Second > 59)
            {
                throw new ArgumentException("Ugyldig tidspunkt");
            }
        }

        // check if date is valid
        if (startDato > slutDato)
        {
            throw new ArgumentException("Ugyldig dato");
        }

        //check if two dosis are at the same time
        for (int i = 0; i < doser.Length; i++)
        {
            for (int j = i + 1; j < doser.Length; j++)
            {
                if (doser[i].tid == doser[j].tid)
                {
                    throw new ArgumentException("To doser kan ikke være på samme tidspunkt");
                }
            }
        }

        // Opret ordinationen
        var dagligSkaev = new DagligSkæv(startDato, slutDato, laegemiddel, doser);

        // Tilføj til patientens ordinationer og databasen
        patient.ordinationer.Add(dagligSkaev);
        db.Ordinationer.Add(dagligSkaev);
        db.SaveChanges();

        return dagligSkaev;
    }

    public string AnvendOrdination(int id, Dato dato) {
        
        var ordination = db.PNs.Include(o => o.dates).FirstOrDefault(o => o.OrdinationId == id);

        if (ordination == null)
        {
            throw new Exception("Ordination ikke fundet");
        }

        //check if ordination exceeds the max

        else if (ordination.startDen > dato.dato || ordination.slutDen < dato.dato)
        {
            return "Dato uden for ordinationens gyldighedsperiode";
        }

        else if (ordination.givDosis(dato)) {
        db.SaveChanges();
            return "Ordination registreret";
        }

        else
        {
            return "Dato allerede registreret";
        }
        
    }

    /// <summary>
    /// Den anbefalede dosis for den pågældende patient, per døgn, hvor der skal tages hensyn til
	/// patientens vægt. Enheden afhænger af lægemidlet. Patient og lægemiddel må ikke være null.
    /// </summary>
    /// <param name="patient"></param>
    /// <param name="laegemiddel"></param>
    /// <returns></returns>
	public double GetAnbefaletDosisPerDøgn(int patientId, int laegemiddelId) {
        Patient patient = db.Patienter.Find(patientId);
        Laegemiddel laegemiddel = db.Laegemiddler.Find(laegemiddelId);

        if (patient.vaegt < 25) 
        { return patient.vaegt * laegemiddel.enhedPrKgPrDoegnLet; }
        
        else if (patient.vaegt >= 25 && patient.vaegt < 120)
        { return patient.vaegt * laegemiddel.enhedPrKgPrDoegnNormal; }
        
        else
        { return patient.vaegt * laegemiddel.enhedPrKgPrDoegnTung; }
        
	}
    
}