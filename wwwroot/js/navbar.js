// Add scroll effect to navbar
window.addEventListener("scroll", function () {
  const navbar = document.querySelector(".modern-navbar");
  if (navbar) {
    if (window.scrollY > 50) {
      navbar.classList.add("scrolled");
    } else {
      navbar.classList.remove("scrolled");
    }
  }
});

// Set active nav link based on current page
document.addEventListener("DOMContentLoaded", function () {
  const currentPath = window.location.pathname;
  const navLinks = document.querySelectorAll(".nav-link");

  navLinks.forEach((link) => {
    const linkPath = link.getAttribute("href");
    if (
      linkPath === currentPath ||
      (currentPath === "/" && linkPath.includes("Index"))
    ) {
      link.classList.add("active");
    } else {
      link.classList.remove("active");
    }
  });
});
