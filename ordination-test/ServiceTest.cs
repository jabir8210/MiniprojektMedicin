namespace ordination_test;

using Microsoft.EntityFrameworkCore;

using Service;
using Data;
using shared.Model;

[TestClass]
public class ServiceTest
{
    private DataService service;

    [TestInitialize]
    public void SetupBeforeEachTest()
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrdinationContext>();
        optionsBuilder.UseInMemoryDatabase(databaseName: "test-database");
        var context = new OrdinationContext(optionsBuilder.Options);
        service = new DataService(context);
        service.SeedData();
    }

    [TestMethod]
    public void PatientsExist()
    {
        Assert.IsNotNull(service.GetPatienter());
    }


    [TestMethod]
    public void OpretDagligFast_Gyldige_Mængder_DF1()
    {
        // Arrange
        Patient patient = service.GetPatienter().First();
        Laegemiddel lm = service.GetLaegemidler().First();

        int initialCount = service.GetDagligFaste().Count();

        // Act
        service.OpretDagligFast(patient.PatientId, lm.LaegemiddelId,
            1, 2, 1, 1, DateTime.Now, DateTime.Now.AddDays(3));

        // Assert
        Assert.AreEqual(initialCount + 1, service.GetDagligFaste().Count(), "DagligFast ordination blev ikke oprettet korrekt.");
    }

    

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void OpretDagligFast_Negative_Mængder_DF2()
    {
        // Arrange
        Patient patient = service.GetPatienter().First();
        Laegemiddel lm = service.GetLaegemidler().First();

        // Act
        service.OpretDagligFast(patient.PatientId, lm.LaegemiddelId,
            -1, 2, -1, -1, DateTime.Now, DateTime.Now.AddDays(3));
    }


    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void OpretDagligFast_Mængde_Nul_DF3()
    {
        // Arrange
        Patient patient = service.GetPatienter().First();
        Laegemiddel lm = service.GetLaegemidler().First();

        // Act
        service.OpretDagligFast(patient.PatientId, lm.LaegemiddelId,
            0, 0, 0, 0, DateTime.Now, DateTime.Now.AddDays(3));
    }



    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void OpretDagligFast_Overskrider_Maks_DF4()
    {
        // Arrange
        Patient patient = service.GetPatienter().First();
        Laegemiddel lm = service.GetLaegemidler().First();

        // Act
        service.OpretDagligFast(patient.PatientId, lm.LaegemiddelId,
            5, 5, 5, 5, DateTime.Now, DateTime.Now.AddDays(3)); // Samlet dosis > maksimum
    }

    
    //daglig fast metoder

    [TestMethod]
    public void DagligFast_DoegnDosis_BeregningErKorrekt()
    {
        // Arrange
        var startDato = new DateTime(2024, 11, 27);
        var slutDato = new DateTime(2024, 11, 30);
        var laegemiddel = new Laegemiddel("TestLaegemiddel", 0.1, 0.15, 0.2, "Styk");

        // Dosismængder pr. dag
        double morgenAntal = 2.0;
        double middagAntal = 1.0;
        double aftenAntal = 3.0;
        double natAntal = 1.5;

        // Opret en DagligFast-ordination
        var dagligFast = new DagligFast(startDato, slutDato, laegemiddel, morgenAntal, middagAntal, aftenAntal, natAntal);

        // Act
        double doegnDosis = dagligFast.doegnDosis();
        double samletDosis = dagligFast.samletDosis();

        // Assert
        Assert.AreEqual(7.5, doegnDosis, "Døgn dosis er ikke beregnet korrekt."); // 2 + 1 + 3 + 1.5 = 7.5
        // Assert
        Assert.AreEqual(30.0, samletDosis, "Samlet dosis er ikke beregnet korrekt."); // (2 + 1 + 3 + 1.5) * 4 dage = 30.0
    }


    [TestMethod]
    public void OpretDagligSkæv_DS1()
    {
        // Arrange
        
        Patient patient = service.GetPatienter().First();
        Laegemiddel laegemiddel = service.GetLaegemidler().First();

        // Definer doser og tidspunkter
        var doser = new Dosis[]
        {
        new Dosis(new DateTime(9, 0), 1),  // 9:00 - 1 dosis
        new Dosis(new DateTime(12, 0), 2), // 12:00 - 2 doser
        new Dosis(new DateTime(18, 0), 1)  // 18:00 - 1 dosis
        };

        DateTime startDato = new DateTime(2024, 11, 27);
        DateTime slutDato = new DateTime(2024, 12, 1);

        // Act
        DagligSkæv dagligSkaev = service.OpretDagligSkaev(patient.PatientId, laegemiddel.LaegemiddelId, doser, startDato, slutDato);

        // Assert
        Assert.IsNotNull(dagligSkaev, "Daglig Skæv ordination blev ikke oprettet.");
        Assert.AreEqual(dagligSkaev.doser.Count, 3, "Antallet af doser matcher ikke.");
        Assert.AreEqual(dagligSkaev.doser[0].antal, 1, "Første dosis mængde er forkert.");
        Assert.AreEqual(dagligSkaev.doser[1].antal, 2, "Anden dosis mængde er forkert.");
        Assert.AreEqual(dagligSkaev.doser[2].antal, 1, "Tredje dosis mængde er forkert.");
    }


    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void OpretDagligSkæv_NegativeDosismængder_KasterException_DS2()
    {
        // Arrange
        Patient patient = service.GetPatienter().First();
        Laegemiddel laegemiddel = service.GetLaegemidler().First();

        // Definer doser med negative værdier
        var doser = new Dosis[]
        {
        new Dosis(new DateTime(9, 0), -1),  // Negativ dosis kl. 09:00
        new Dosis(new DateTime(12, 0), -2), // Negativ dosis kl. 12:00
        new Dosis(new DateTime(18, 0), -1)  // Negativ dosis kl. 18:00
        };

        DateTime startDato = new DateTime(2024, 11, 27);
        DateTime slutDato = new DateTime(2024, 12, 1);

        // Act
        // Denne linje forventes at kaste en ArgumentException på grund af negative dosismængder
        service.OpretDagligSkaev(patient.PatientId, laegemiddel.LaegemiddelId, doser, startDato, slutDato);
    }


    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void OpretDagligSkæv_UnikkeTidspunkter_KasterException_DS3()
    {
        // Arrange
        Patient patient = service.GetPatienter().First();
        Laegemiddel laegemiddel = service.GetLaegemidler().First();

        // Definer doser med negative værdier
        var doser = new Dosis[]
        {
        new Dosis(new DateTime(9, 0), 1),  //dosis kl. 09:00
        new Dosis(new DateTime(9, 0), 2), // dosis kl. 09:00 igen
        new Dosis(new DateTime(18, 0), 1)  // dosis kl. 25:00
        };

        DateTime startDato = new DateTime(2024, 11, 27);
        DateTime slutDato = new DateTime(2024, 12, 1);

        // Act
        // Denne linje forventes at kaste en ArgumentException på grund af negative dosismængder
        service.OpretDagligSkaev(patient.PatientId, laegemiddel.LaegemiddelId, doser, startDato, slutDato);
    }

    //daglig skæv metoder
    [TestMethod]
    public void DagligSkæv_DoegnOgSamletDosis_BeregningErKorrekt()
    {
        // Arrange
        var startDato = new DateTime(2024, 11, 27);
        var slutDato = new DateTime(2024, 11, 30); // 4 dage
        var laegemiddel = new Laegemiddel("TestLaegemiddel", 0.1, 0.15, 0.2, "Styk");

        // Opret doser (skæve tidspunkter og mængder)
        var doser = new Dosis[]
        {
        new Dosis(new DateTime(8, 0), 1.0), // 1 dosis kl. 8:00
        new Dosis(new DateTime(12, 0), 2.5), // 2.5 doser kl. 12:00
        new Dosis(new DateTime(20, 0), 1.5)  // 1.5 doser kl. 20:00
        };

        // Opret en DagligSkæv-ordination
        var dagligSkæv = new DagligSkæv(startDato, slutDato, laegemiddel, doser);

        // Act
        double doegnDosis = dagligSkæv.doegnDosis(); // Beregn døgn dosis
        double samletDosis = dagligSkæv.samletDosis(); // Beregn samlet dosis

        // Assert
        Assert.AreEqual(5.0, doegnDosis, "Døgn dosis er ikke beregnet korrekt."); // 1 + 2.5 + 1.5 = 5.0
        Assert.AreEqual(20.0, samletDosis, "Samlet dosis er ikke beregnet korrekt."); // 5.0 * 4 dage = 20.0
    }


    [TestMethod]
    public void OpretPN_GyldigDosis_PN1()
    {
        // Arrange
        Patient patient = service.GetPatienter().First();
        Laegemiddel laegemiddel = service.GetLaegemidler().First();

        DateTime startDato = new DateTime(2024, 11, 27);
        DateTime slutDato = new DateTime(2024, 12, 1);
        double dosis = 5; // Gyldig dosis

        // Act
        var pnOrdination = service.OpretPN(patient.PatientId, laegemiddel.LaegemiddelId, dosis, startDato, slutDato);

        // Assert
        Assert.IsNotNull(pnOrdination, "PN-ordinationen blev ikke oprettet.");
        Assert.AreEqual(dosis, pnOrdination.antalEnheder, "Dosis er ikke korrekt.");
        Assert.AreEqual(startDato, pnOrdination.startDen, "Startdato er ikke korrekt.");
        Assert.AreEqual(slutDato, pnOrdination.slutDen, "Slutdato er ikke korrekt.");
        Assert.AreEqual(laegemiddel, pnOrdination.laegemiddel, "Lægemidlet er ikke korrekt.");
    }

    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void OpretPN_Negativ_KasterException_PN2()
    {
        // Arrange
        Patient patient = service.GetPatienter().First();
        Laegemiddel laegemiddel = service.GetLaegemidler().First();
        
        double invalidDosis = -1; // Negativ dosis
        
        DateTime startDato = new DateTime(2024, 11, 27);
        DateTime slutDato = new DateTime(2024, 12, 1);

        // Act
        // Forvent at metoden kaster en ArgumentException
        service.OpretPN(patient.PatientId, laegemiddel.LaegemiddelId, invalidDosis, startDato, slutDato);
    }


    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void OpretPN_Nul_Dosis_KasterException_PN3()
    {
        // Arrange
        Patient patient = service.GetPatienter().First();
        Laegemiddel laegemiddel = service.GetLaegemidler().First();

        double invalidDosis = 0; // Nul dosis

        DateTime startDato = new DateTime(2024, 11, 27);
        DateTime slutDato = new DateTime(2024, 12, 1);

        // Act
        // Forvent at metoden kaster en ArgumentException
        service.OpretPN(patient.PatientId, laegemiddel.LaegemiddelId, invalidDosis, startDato, slutDato);
    }


    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void OpretPN_OverskridelseAfAnbefaletDosis_KasterException_PN4()
    {
        // Arrange
        Patient patient = service.GetPatienter().First();
        Laegemiddel laegemiddel = service.GetLaegemidler().First();

        double invalidDosis = service.GetAnbefaletDosisPerDøgn(patient.PatientId, laegemiddel.LaegemiddelId) + 10; // Over anbefalet dosis
        DateTime startDato = new DateTime(2024, 11, 27);
        DateTime slutDato = new DateTime(2024, 12, 1);

        // Act
        // Forvent at metoden kaster en ArgumentException
        service.OpretPN(patient.PatientId, laegemiddel.LaegemiddelId, invalidDosis, startDato, slutDato);
    }


    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void OpretPN_UgyldigGyldighedsperiode_KasterException_PN5()
    {
        // Arrange
        Patient patient = service.GetPatienter().First();
        Laegemiddel laegemiddel = service.GetLaegemidler().First();
       
        double dosis = 5;

        DateTime startDato = new DateTime(2024, 12, 1); // Startdato efter slutdato
        DateTime slutDato = new DateTime(2024, 11, 27);

        // Act
        // Forvent at metoden kaster en ArgumentException
        service.OpretPN(patient.PatientId, laegemiddel.LaegemiddelId, dosis, startDato, slutDato);
    }



    [TestMethod]
    [ExpectedException(typeof(ArgumentException))]
    public void OpretPN_IngenDoserRegistreret_KasterException_PN6()
    {
        // Arrange
        Patient patient = service.GetPatienter().First();
        Laegemiddel laegemiddel = service.GetLaegemidler().First();

        DateTime startDato = new DateTime(2024, 11, 27);
        DateTime slutDato = new DateTime(2024, 11, 29); // Ingen periode, ingen doser

        // Act
        // Denne linje forventes at kaste en ArgumentException
        service.OpretPN(patient.PatientId, laegemiddel.LaegemiddelId, 0, startDato, slutDato);
    }

    //PN metoder
    [TestMethod]
    public void PN_DoegnOgSamletDosis_BeregningErKorrekt()
    {
        // Arrange
        var startDato = new DateTime(2024, 11, 27);
        var slutDato = new DateTime(2024, 12, 1); // 5 dage
        var laegemiddel = new Laegemiddel("TestLaegemiddel", 0.1, 0.15, 0.2, "Styk");
        double antalEnheder = 2.0; // Hver dosis er 2 enheder

        // Opret en PN-ordination
        var pn = new PN(startDato, slutDato, antalEnheder, laegemiddel);

        // Tilføj doser med kun dato-delen (uden tid)
        pn.givDosis(new Dato { dato = new DateTime(2024, 11, 27).Date }); // Dag 1
        pn.givDosis(new Dato { dato = new DateTime(2024, 11, 28).Date }); // Dag 2
        pn.givDosis(new Dato { dato = new DateTime(2024, 11, 29).Date }); // Dag 3
        pn.givDosis(new Dato { dato = new DateTime(2024, 11, 30).Date }); // Dag 4

        // Act
        double samletDosis = pn.samletDosis(); // Beregn samlet dosis
        double doegnDosis = pn.doegnDosis();   // Beregn døgn dosis

        // Assert
        Assert.AreEqual(8.0, samletDosis, "Samlet dosis er ikke beregnet korrekt."); // 3 doser * 2 enheder = 6.0
        Assert.AreEqual(2.0, doegnDosis, "Døgn dosis er ikke beregnet korrekt."); // 6.0 / 3 dage = 2.0
    }




}